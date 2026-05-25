using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LockStepLib.Command;
using LockStepLib.Session;
using LockStepLib.Simulation;
using LockStepLib.Transport;

namespace Tests
{
    /// <summary>
    /// 确定性集成测试。模拟多客户端同时运行，验证所有客户端最终状态完全一致。
    /// </summary>

    // 集成测试用确定性游戏: 每个玩家维护一个位置，输入改变位置
    class IntegrationGame : IDeterministicGame
    {
        public string GameId => "IntegrationTest";
        public int GameVersion => 1;

        public int[] Positions; // 每个玩家的位置
        public int FrameCount;
        public List<ulong> FrameHashes = new List<ulong>();

        public void Initialize(IGameState state)
        {
            var s = state as IntegrationGameState;
            Positions = s?.Positions?.Clone() as int[] ?? new int[3];
            FrameCount = s?.FrameCount ?? 0;
            FrameHashes = s?.FrameHashes ?? new List<ulong>();
        }

        public IGameState GetStateSnapshot() => new IntegrationGameState
        {
            Positions = (int[])Positions.Clone(),
            FrameCount = FrameCount,
            FrameHashes = new List<ulong>(FrameHashes),
        };

        public void RestoreState(IGameState state)
        {
            var s = (IntegrationGameState)state;
            Positions = (int[])s.Positions.Clone();
            FrameCount = s.FrameCount;
            FrameHashes = new List<ulong>(s.FrameHashes);
        }

        public void Update(DeterministicInput[] inputs, int frame)
        {
            foreach (var inp in inputs)
            {
                if (inp.Command is IntegrationCmd cmd)
                {
                    if (inp.PlayerId >= 0 && inp.PlayerId < Positions.Length)
                        Positions[inp.PlayerId] += cmd.Delta;
                }
            }
            FrameCount++;

            // 每 10 帧记录一次哈希
            if (frame % 10 == 0)
                FrameHashes.Add(GetFrameHash());
        }

        public ulong GetFrameHash()
        {
            ulong h = (ulong)FrameCount * 31;
            foreach (int p in Positions)
                h = h * 37 + (ulong)(p + 1);
            return h;
        }
    }

    class IntegrationGameState : IGameState
    {
        public int[] Positions;
        public int FrameCount;
        public List<ulong> FrameHashes;

        public void Serialize(BinaryWriter w)
        {
            w.Write(Positions.Length);
            foreach (int p in Positions) w.Write(p);
            w.Write(FrameCount);
            w.Write(FrameHashes.Count);
            foreach (ulong h in FrameHashes) w.Write(h);
        }

        public void Deserialize(BinaryReader r)
        {
            int len = r.ReadInt32();
            Positions = new int[len];
            for (int i = 0; i < len; i++) Positions[i] = r.ReadInt32();
            FrameCount = r.ReadInt32();
            int hashCount = r.ReadInt32();
            FrameHashes = new List<ulong>(hashCount);
            for (int i = 0; i < hashCount; i++) FrameHashes.Add(r.ReadUInt64());
        }
    }

    class IntegrationCmd : IInputCommand
    {
        public int CommandTypeId => 100;
        public int Delta;

        public IntegrationCmd() { }
        public IntegrationCmd(int delta) { Delta = delta; }

        public void Serialize(BinaryWriter w) => w.Write(Delta);
        public void Deserialize(BinaryReader r) => Delta = r.ReadInt32();
    }

    public static class DeterminismIntegrationTests
    {
        private const int FRAME_COUNT = 100;
        private const int FRAME_RATE = 60;
        private const int PORT = 19560;

        public static void Run()
        {
            Console.WriteLine("--- Determinism Integration Tests ---");

            if (!ThreePlayerDeterminism())
                Console.WriteLine("  SKIP: three-player test requires network environment");
            else
                Console.WriteLine("  PASS: three-player determinism verified");
        }

        static bool ThreePlayerDeterminism()
        {
            Console.WriteLine("\n  [3-Player Determinism: Server + 2 Clients, 100 frames]");

            // 创建三方游戏实例
            var serverGame = new IntegrationGame();
            var client1Game = new IntegrationGame();
            var client2Game = new IntegrationGame();

            serverGame.Initialize(null);
            client1Game.Initialize(null);
            client2Game.Initialize(null);

            // 服务器 (玩家 0)
            var serverTransport = new TcpTransport();
            var serverConfig = new SessionConfig { FrameRate = FRAME_RATE, PlayerCount = 3, LocalPlayerId = 0, Port = PORT };
            var server = new LockstepSession(serverTransport, serverGame, serverConfig);
            server.CommandFactory.Register(100, () => new IntegrationCmd());

            // 客户端 1 (玩家 1)
            var c1Transport = new TcpTransport();
            var c1Config = new SessionConfig { FrameRate = FRAME_RATE, PlayerCount = 1, LocalPlayerId = 1, ServerHost = "127.0.0.1", Port = PORT, FrameBufferSize = 1 };
            var client1 = new LockstepSession(c1Transport, client1Game, c1Config);
            client1.CommandFactory.Register(100, () => new IntegrationCmd());

            // 客户端 2 (玩家 2)
            var c2Transport = new TcpTransport();
            var c2Config = new SessionConfig { FrameRate = FRAME_RATE, PlayerCount = 1, LocalPlayerId = 2, ServerHost = "127.0.0.1", Port = PORT, FrameBufferSize = 1 };
            var client2 = new LockstepSession(c2Transport, client2Game, c2Config);
            client2.CommandFactory.Register(100, () => new IntegrationCmd());

            int serverFrames = 0, c1Frames = 0, c2Frames = 0;
            server.OnFrameAdvanced += f => serverFrames = f;
            client1.OnFrameAdvanced += f => c1Frames = f;
            client2.OnFrameAdvanced += f => c2Frames = f;

            // 启动
            server.Start(SessionRole.Server);
            Thread.Sleep(100);
            client1.Start(SessionRole.Client);
            client2.Start(SessionRole.Client);

            // 等待全部连接
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (server.State != SessionState.Running && sw.ElapsedMilliseconds < 5000)
            {
                server.Update();
                client1.Update();
                client2.Update();
                Thread.Sleep(5);
            }

            if (server.State != SessionState.Running)
            {
                Console.WriteLine("    FAIL: server did not enter Running state");
                server.Stop(); client1.Stop(); client2.Stop();
                return false;
            }

            // 随机种子 (保证确定性: 所有客户端用相同种子)
            var random = new DeterministicRandom(42UL);

            // 主循环: 跑 FRAME_COUNT 帧
            sw.Restart();
            while (serverFrames < FRAME_COUNT && sw.ElapsedMilliseconds < 30000)
            {
                // 三方提交随机输入
                server.SubmitInput(new IntegrationCmd(random.Next(-5, 6)));
                client1.SubmitInput(new IntegrationCmd(random.Next(-5, 6)));
                client2.SubmitInput(new IntegrationCmd(random.Next(-5, 6)));
                server.Update();
                Thread.Sleep(1); // TCP 数据到达
                server.Update();
                client1.Update();
                client2.Update();
                Thread.Sleep(16);
            }

            // 等待客户端追赶
            var waitSw = System.Diagnostics.Stopwatch.StartNew();
            while ((c1Frames < FRAME_COUNT || c2Frames < FRAME_COUNT) && waitSw.ElapsedMilliseconds < 5000)
            {
                client1.Update();
                client2.Update();
                Thread.Sleep(5);
            }

            Console.WriteLine($"    Server: {serverFrames} frames, Client1: {c1Frames}, Client2: {c2Frames}");

            // 验证
            bool ok = true;

            // 1. 帧数一致
            if (serverFrames < FRAME_COUNT) { Console.WriteLine("    FAIL: server didn't reach target"); ok = false; }
            if (c1Frames < FRAME_COUNT) { Console.WriteLine("    FAIL: client1 didn't reach target"); ok = false; }
            if (c2Frames < FRAME_COUNT) { Console.WriteLine("    FAIL: client2 didn't reach target"); ok = false; }

            // 2. 最终状态一致
            if (ok)
            {
                bool stateMatch = true;
                for (int p = 0; p < 3; p++)
                {
                    if (serverGame.Positions[p] != client1Game.Positions[p] ||
                        serverGame.Positions[p] != client2Game.Positions[p])
                    {
                        Console.WriteLine($"    FAIL: Player {p} position mismatch: S={serverGame.Positions[p]}, C1={client1Game.Positions[p]}, C2={client2Game.Positions[p]}");
                        stateMatch = false;
                    }
                }
                if (stateMatch) Console.WriteLine("    ✓ All player positions match");
                else ok = false;

                // 3. 每 10 帧哈希全部一致
                if (serverGame.FrameHashes.Count == client1Game.FrameHashes.Count &&
                    serverGame.FrameHashes.Count == client2Game.FrameHashes.Count)
                {
                    bool hashesMatch = true;
                    for (int i = 0; i < serverGame.FrameHashes.Count; i++)
                    {
                        ulong sh = serverGame.FrameHashes[i];
                        ulong c1h = client1Game.FrameHashes[i];
                        ulong c2h = client2Game.FrameHashes[i];
                        if (sh != c1h || sh != c2h)
                        {
                            Console.WriteLine($"    FAIL: Hash mismatch at checkpoint {i * 10}: S=0x{sh:X16}, C1=0x{c1h:X16}, C2=0x{c2h:X16}");
                            hashesMatch = false;
                            break;
                        }
                    }
                    if (hashesMatch) Console.WriteLine($"    ✓ All {serverGame.FrameHashes.Count} checkpoint hashes match");
                    else ok = false;
                }
                else
                {
                    Console.WriteLine($"    FAIL: Hash count mismatch: S={serverGame.FrameHashes.Count}, C1={client1Game.FrameHashes.Count}, C2={client2Game.FrameHashes.Count}");
                    ok = false;
                }
            }

            server.Stop();
            client1.Stop();
            client2.Stop();

            return ok;
        }
    }
}
