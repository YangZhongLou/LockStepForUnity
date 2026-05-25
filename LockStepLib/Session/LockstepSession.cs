using System;
using System.Collections.Generic;
using LockStepLib.Command;
using LockStepLib.Core;
using LockStepLib.Simulation;
using LockStepLib.Transport;

namespace LockStepLib.Session
{
    /// <summary>
    /// 帧同步会话核心编排器。协调传输层、指令收集、帧推进和游戏仿真。
    /// 支持 Server/Client/Spectator 三种角色。
    /// </summary>
    public class LockstepSession : IDisposable
    {
        private readonly INetworkTransport _transport;
        private readonly IDeterministicGame _game;
        private readonly SessionConfig _config;
        private readonly CommandSerializer _serializer;
        private readonly CommandBuffer _commandBuffer;
        private readonly FrameSynchronizer _synchronizer;
        private readonly GameLoop _gameLoop;

        // 玩家连接映射
        private readonly Dictionary<int, PlayerConnection> _players = new Dictionary<int, PlayerConnection>();
        private readonly Dictionary<int, IConnection> _idToConnection = new Dictionary<int, IConnection>();

        // 服务器端：当前正在收集输入的帧号
        private int _collectingFrame;

        private SessionRole _role;
        private SessionState _state;
        private bool _running;

        #region 事件

        /// <summary>帧推进</summary>
        public event Action<int> OnFrameAdvanced;

        /// <summary>状态变化</summary>
        public event Action<SessionState> OnStateChanged;

        /// <summary>一致性校验失败 (frame, localHash, remoteHash)</summary>
        public event Action<int, ulong, ulong> OnDesyncDetected;

        /// <summary>玩家断开 (playerId, reason)</summary>
        public event Action<int, string> OnPlayerDisconnected;

        #endregion

        #region 属性

        public int CurrentFrame => _gameLoop.CurrentFrame;
        public SessionState State => _state;
        public int FrameRate => _config.FrameRate;
        public int ConnectedPlayers => _players.Count;

        /// <summary>指令工厂 (Start 前注册游戏指令类型)</summary>
        public CommandFactory CommandFactory => _serializer.Factory;

        #endregion

        public LockstepSession(INetworkTransport transport, IDeterministicGame game, SessionConfig config)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _serializer = new CommandSerializer(new CommandFactory());
            _commandBuffer = new CommandBuffer();
            _synchronizer = new FrameSynchronizer(config.FrameBufferSize);
            _gameLoop = new GameLoop();
        }

        #region 生命周期

        /// <summary>启动会话</summary>
        public void Start(SessionRole role)
        {
            _role = role;
            SetState(role == SessionRole.Server ? SessionState.Connecting : SessionState.Connecting);

            _transport.OnConnected += OnTransportConnected;
            _transport.OnDisconnected += OnTransportDisconnected;

            switch (role)
            {
                case SessionRole.Server:
                    _transport.OnDataReceived += OnServerDataReceived;
                    _transport.StartServer(_config.Port, _config.PlayerCount);
                    _collectingFrame = 0;
                    // 将本地玩家加入列表
                    _players[_config.LocalPlayerId] = new PlayerConnection(_config.LocalPlayerId, null);
                    _game.Initialize(null);
                    LogManager.Info($"Server: port={_config.Port}, local=P{_config.LocalPlayerId}, expecting {_config.PlayerCount} players");
                    break;
                case SessionRole.Client:
                case SessionRole.Spectator:
                    _transport.OnDataReceived += OnClientDataReceived;
                    _transport.Connect(_config.ServerHost, _config.Port);
                    LogManager.Info($"Client: connecting to {_config.ServerHost}:{_config.Port}");
                    break;
            }
        }

        /// <summary>停止会话</summary>
        public void Stop()
        {
            _running = false;
            _gameLoop.Stop();
            _synchronizer.Reset();
            _commandBuffer.Clear();
            _transport.Shutdown();
            SetState(SessionState.Finished);
        }

        #endregion

        #region 主更新

        /// <summary>每帧调用</summary>
        public void Update()
        {
            _transport.Update();
            if (_state == SessionState.Idle || _state == SessionState.Finished) return;

            switch (_role)
            {
                case SessionRole.Server: ServerUpdate(); break;
                case SessionRole.Client: ClientSimulate(); break;
                case SessionRole.Spectator: ClientSimulate(); break;
            }
        }

        #endregion

        #region 服务器逻辑

        private void ServerUpdate()
        {
            if (_state == SessionState.Connecting)
            {
                if (_players.Count >= _config.PlayerCount)
                {
                    SetState(SessionState.Running);
                    _gameLoop.Start(_config.FrameRate);
                    _running = true;
                    LogManager.Info($"All players ready, game started");
                    // 不 return，直接进入 Running 逻辑处理当前帧
                }
                else return;
            }

            if (_state != SessionState.Running || !_running) return;

            // 收集当前帧输入 (只检查活跃玩家)
            if (AllActivePlayersReady(_collectingFrame))
            {
                var inputs = GetAllActiveInputs(_collectingFrame);

                // 冗余输入
                var redundancy = new List<DeterministicInput>();
                for (int f = _collectingFrame - 1; f > _collectingFrame - 1 - _config.InputRedundancy && f > 0; f--)
                    redundancy.AddRange(GetAllActiveInputs(f));

                // 一致性数据
                ConsistencyData? consistency = null;
                if (_config.ConsistencyCheckInterval > 0 && _collectingFrame % _config.ConsistencyCheckInterval == 0)
                    consistency = new ConsistencyData(_collectingFrame, _game.GetFrameHash());

                var pkg = new FramePackage(_collectingFrame, inputs,
                    redundancy.Count > 0 ? redundancy.ToArray() : null, consistency);

                byte[] data = _serializer.SerializeFramePackage(pkg);
                _transport.Broadcast(data, 0, data.Length, SendOptions.Reliable);

                _game.Update(inputs, _collectingFrame);
                _gameLoop.FastForward(1);
                OnFrameAdvanced?.Invoke(_collectingFrame);
                _collectingFrame++;
            }
        }

        private void OnServerDataReceived(IConnection conn, ArraySegment<byte> data)
        {
            try
            {
                var input = _serializer.DeserializeInput(data.ToArray());
                _commandBuffer.AddInput(input.PlayerId, input.Frame, input.Command);
            }
            catch (Exception ex) { LogManager.Error($"Server recv: {ex.Message}"); }
        }

        #endregion

        #region 客户端逻辑

        private void ClientSimulate()
        {
            if (!_running)
            {
                // 等待首个帧包到达
                return;
            }

            if (_state != SessionState.Running) return;

            int framesToAdvance = _gameLoop.Update();
            for (int i = 0; i < framesToAdvance; i++)
            {
                if (_synchronizer.ShouldAdvance())
                {
                    var pkg = _synchronizer.Dequeue();
                    if (pkg.HasValue)
                    {
                        _game.Update(pkg.Value.PrimaryInputs, pkg.Value.Frame);
                        OnFrameAdvanced?.Invoke(pkg.Value.Frame);

                        if (pkg.Value.Consistency.HasValue)
                        {
                            ulong local = _game.GetFrameHash();
                            ulong remote = pkg.Value.Consistency.Value.FrameHash;
                            if (local != remote)
                                OnDesyncDetected?.Invoke(pkg.Value.Frame, local, remote);
                        }
                    }
                }
                else
                {
                    _synchronizer.DynamicAdjust();
                    break;
                }
            }
        }

        private void OnClientDataReceived(IConnection conn, ArraySegment<byte> data)
        {
            try
            {
                var pkg = _serializer.DeserializeFramePackage(data.ToArray());
                _synchronizer.Enqueue(pkg);

                // 也存入 CommandBuffer (去重)
                if (pkg.RedundancyInputs != null)
                    foreach (var inp in pkg.RedundancyInputs)
                        _commandBuffer.AddInput(inp.PlayerId, inp.Frame, inp.Command);
                foreach (var inp in pkg.PrimaryInputs)
                    _commandBuffer.AddInput(inp.PlayerId, inp.Frame, inp.Command);

                // 首个帧包 → 开始运行
                if (!_running)
                {
                    _gameLoop.Start(_config.FrameRate);
                    _running = true;
                    SetState(SessionState.Running);
                }
            }
            catch (Exception ex) { LogManager.Error($"Client recv: {ex.Message}"); }
        }

        #endregion

        #region 输入提交

        /// <summary>提交本地玩家输入</summary>
        public void SubmitInput(IInputCommand command)
        {
            // 观战者不能提交输入
            if (_role == SessionRole.Spectator) return;
            if (_state != SessionState.Running && _state != SessionState.Connecting) return;

            int frame = _role == SessionRole.Server ? _collectingFrame : _synchronizer.NextFrame;

            if (_role == SessionRole.Server)
            {
                _commandBuffer.AddInput(_config.LocalPlayerId, frame, command);
            }
            else if (_players.TryGetValue(_config.LocalPlayerId, out var local) && local.Connection != null)
            {
                var input = new DeterministicInput(_config.LocalPlayerId, frame, command);
                byte[] data = _serializer.SerializeInput(input);
                _transport.Send(local.Connection, data, 0, data.Length, SendOptions.Reliable);
            }
        }

        #endregion

        #region 连接回调

        private void OnTransportConnected(IConnection conn)
        {
            if (_role == SessionRole.Server)
            {
                int pid = _players.Count;
                _players[pid] = new PlayerConnection(pid, conn);
                _idToConnection[conn.Id] = conn;
                LogManager.Debug($"Player {pid} connected ({_players.Count}/{_config.PlayerCount})");
            }
            else
            {
                _players[_config.LocalPlayerId] = new PlayerConnection(_config.LocalPlayerId, conn);
                _idToConnection[conn.Id] = conn;
            }
        }

        private void OnTransportDisconnected(IConnection conn, string reason)
        {
            foreach (var kv in _players)
            {
                if (kv.Value.Connection == conn)
                {
                    kv.Value.Disconnected = true;
                    OnPlayerDisconnected?.Invoke(kv.Key, reason);
                    break;
                }
            }
        }

        #endregion

        #region 辅助

        private bool AllActivePlayersReady(int frame)
        {
            for (int p = 0; p < _config.PlayerCount; p++)
            {
                if (_players.TryGetValue(p, out var pc) && pc.Disconnected) continue;
                if (!_commandBuffer.HasInput(p, frame)) return false;
            }
            return true;
        }

        private DeterministicInput[] GetAllActiveInputs(int frame)
        {
            return _commandBuffer.GetInputsForFrame(frame, _config.PlayerCount);
        }

        private void SetState(SessionState s)
        {
            if (_state == s) return;
            _state = s;
            OnStateChanged?.Invoke(s);
        }

        #endregion

        public void Dispose() => Stop();
    }
}
