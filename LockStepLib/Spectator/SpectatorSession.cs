using System;
using LockStepLib.Command;
using LockStepLib.Session;
using LockStepLib.Simulation;
using LockStepLib.Transport;

namespace LockStepLib.Spectator
{
    /// <summary>
    /// 观战会话。作为纯观察者连接到服务器，接收帧包但不提交输入。
    /// 支持迟加入：服务器发送状态快照 + 已累积的输入，客户端恢复状态后快进。
    /// </summary>
    public class SpectatorSession
    {
        private readonly LockstepSession _session;
        private readonly string _host;
        private readonly int _port;

        /// <summary>观战是否正在运行</summary>
        public bool IsRunning => _session.State == SessionState.Running;

        /// <summary>当前帧</summary>
        public int CurrentFrame => _session.CurrentFrame;

        /// <summary>帧推进事件</summary>
        public event Action<int> OnFrameAdvanced
        {
            add => _session.OnFrameAdvanced += value;
            remove => _session.OnFrameAdvanced -= value;
        }

        public SpectatorSession(INetworkTransport transport, IDeterministicGame game, string host, int port)
        {
            _host = host;
            _port = port;

            var config = new SessionConfig
            {
                FrameRate = 30,
                ServerHost = host,
                Port = port,
                FrameBufferSize = 2,
            };

            _session = new LockstepSession(transport, game, config);
        }

        /// <summary>连接到游戏并开始观战</summary>
        public void Connect()
        {
            _session.Start(SessionRole.Spectator);
        }

        /// <summary>每帧更新</summary>
        public void Update()
        {
            _session.Update();
        }

        /// <summary>停止观战</summary>
        public void Stop()
        {
            _session.Stop();
        }
    }
}
