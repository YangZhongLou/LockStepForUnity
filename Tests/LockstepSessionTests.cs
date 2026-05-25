using System;
using System.IO;
using System.Threading;
using LockStepLib.Command;
using LockStepLib.Session;
using LockStepLib.Simulation;
using LockStepLib.Transport;

namespace Tests
{
    // 测试用简单游戏
    class TestGame : IDeterministicGame
    {
        public string GameId => "TestGame";
        public int GameVersion => 1;

        public int Counter;
        private ulong _lastHash;

        public void Initialize(IGameState state) { Counter = 0; }
        public IGameState GetStateSnapshot() => new TestGameState { Counter = Counter };
        public void RestoreState(IGameState state) { Counter = ((TestGameState)state).Counter; }

        public void Update(DeterministicInput[] inputs, int frame)
        {
            foreach (var inp in inputs)
            {
                if (inp.Command is TestInputCmd cmd)
                    Counter += cmd.Delta;
            }
            _lastHash = (ulong)(Counter * 31 + frame);
        }

        public ulong GetFrameHash() => _lastHash;
    }

    class TestGameState : IGameState
    {
        public int Counter;
        public void Serialize(BinaryWriter w) => w.Write(Counter);
        public void Deserialize(BinaryReader r) => Counter = r.ReadInt32();
    }

    class TestInputCmd : IInputCommand
    {
        public int CommandTypeId => 1;
        public int Delta;

        public TestInputCmd() { }
        public TestInputCmd(int delta) { Delta = delta; }

        public void Serialize(BinaryWriter w) => w.Write(Delta);
        public void Deserialize(BinaryReader r) => Delta = r.ReadInt32();
    }

    public static class LockstepSessionTests
    {
        public static void Run()
        {
            Console.WriteLine("--- LockstepSession Tests ---");

            ServerCollectAndBroadcast();
            ClientReceiveAndSimulate();
            FrameSynchronizerContract();
        }

        static void ServerCollectAndBroadcast()
        {
            TestRunner.StartSection("Server Collect & Broadcast");

            var transport = new TcpTransport();
            var game = new TestGame();
            var config = new SessionConfig { FrameRate = 30, PlayerCount = 1, LocalPlayerId = 0 };
            var session = new LockstepSession(transport, game, config);
            session.CommandFactory.Register(1, () => new TestInputCmd());

            int lastFrame = -1;
            session.OnFrameAdvanced += f => lastFrame = f;

            session.Start(SessionRole.Server);

            // 提交输入直到达到 10 帧
            int targetFrame = 10;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (lastFrame < targetFrame && sw.ElapsedMilliseconds < 3000)
            {
                session.SubmitInput(new TestInputCmd(5));
                session.Update();
                Thread.Sleep(2);
            }

            TestRunner.Assert(lastFrame >= targetFrame, $"server reached frame {lastFrame} (target {targetFrame})");
            // 帧号从 0 开始，共 targetFrame+1 帧
            TestRunner.AssertEqual((targetFrame + 1) * 5, game.Counter, $"{(targetFrame + 1)} * 5 = {(targetFrame + 1) * 5} counter");

            session.Stop();
        }

        static void ClientReceiveAndSimulate()
        {
            TestRunner.StartSection("Client Receive & Simulate");

            // 启动服务器
            var serverTransport = new TcpTransport();
            var serverGame = new TestGame();
            var serverConfig = new SessionConfig { FrameRate = 60, PlayerCount = 1, LocalPlayerId = 0, Port = 19552 };
            var server = new LockstepSession(serverTransport, serverGame, serverConfig);
            server.CommandFactory.Register(1, () => new TestInputCmd());

            // 启动客户端
            var clientTransport = new TcpTransport();
            var clientGame = new TestGame();
            var clientConfig = new SessionConfig { FrameRate = 60, PlayerCount = 1, ServerHost = "127.0.0.1", Port = 19552, FrameBufferSize = 2 };
            var client = new LockstepSession(clientTransport, clientGame, clientConfig);
            client.CommandFactory.Register(1, () => new TestInputCmd());

            int serverFrames = 0;
            int clientFrames = 0;
            server.OnFrameAdvanced += f => serverFrames++;
            client.OnFrameAdvanced += f => clientFrames++;

            server.Start(SessionRole.Server);
            Thread.Sleep(50);
            client.Start(SessionRole.Client);
            Thread.Sleep(100);

            // 跑 20 帧
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (serverFrames < 20 && sw.ElapsedMilliseconds < 5000)
            {
                server.SubmitInput(new TestInputCmd(3));
                server.Update();
                client.Update();
                Thread.Sleep(5);
            }

            TestRunner.Assert(serverFrames >= 20, $"server ran {serverFrames} frames");

            server.Stop();
            client.Stop();
        }

        static void FrameSynchronizerContract()
        {
            TestRunner.StartSection("FrameSynchronizer Contract");

            var sync = new LockStepLib.Session.FrameSynchronizer(2);
            // 缓冲只有 frame 3, NextFrame=0 → 不推进
            sync.Enqueue(new FramePackage(3, new DeterministicInput[0]));
            TestRunner.AssertEqual(false, sync.ShouldAdvance(), "only F3, no F0 → false");

            // 加 frame 0 → NextFrame 就绪, 推进
            sync.Enqueue(new FramePackage(0, new DeterministicInput[0]));
            TestRunner.AssertEqual(true, sync.ShouldAdvance(), "F0 arrived → true");

            // Dequeue 后 NextFrame=1, 缓冲有 F1 和 F3 → F1 就绪
            sync.Dequeue();
            sync.Enqueue(new FramePackage(1, new DeterministicInput[0]));
            TestRunner.AssertEqual(true, sync.ShouldAdvance(), "F1 present → true");
        }
    }
}
