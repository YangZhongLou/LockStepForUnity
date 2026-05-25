using System;
using System.IO;
using LockStepLib.Command;
using LockStepLib.Replay;

namespace Tests
{
    public static class ReplayTests
    {
        private static CommandSerializer _serializer;
        private static CommandFactory _factory;

        public static void Run()
        {
            Console.WriteLine("--- Replay Tests ---");

            _factory = new CommandFactory();
            _factory.Register(1, () => new TestInputCmd());
            _serializer = new CommandSerializer(_factory);

            RecordAndPlayback();
            FileHeader();
        }

        static void RecordAndPlayback()
        {
            TestRunner.StartSection("Record & Playback");

            var metadata = new ReplayMetadata
            {
                GameId = "TestGame",
                GameVersion = 1,
                FrameRate = 30,
                PlayerCount = 2,
                StartTime = DateTime.UtcNow,
            };

            string path = Path.Combine(Path.GetTempPath(), "test_replay.lsrp");

            // 录制
            var recorder = new ReplayRecorder(_serializer, metadata);
            recorder.Start(path);

            for (int f = 1; f <= 20; f++)
            {
                var inputs = new DeterministicInput[]
                {
                    new DeterministicInput(0, f, new TestInputCmd(f * 10)),
                    new DeterministicInput(1, f, new TestInputCmd(f * 10 + 1)),
                };
                recorder.Record(new FramePackage(f, inputs));
            }
            recorder.Stop();

            TestRunner.AssertEqual(20, recorder.RecordedFrames, "recorded 20 frames");

            // 播放
            var player = new ReplayPlayer(_serializer);
            bool loaded = player.Load(path);
            TestRunner.AssertEqual(true, loaded, "load replay file");
            TestRunner.AssertEqual(20, player.TotalFrames, "20 frames in replay");

            // 验证内容
            var pkg = player.GetFrame(5);
            TestRunner.Assert(pkg.HasValue, "frame 5 exists");
            TestRunner.AssertEqual(5, pkg.Value.Frame, "frame number = 5");
            TestRunner.AssertEqual(2, pkg.Value.PrimaryInputs.Length, "2 inputs in frame 5");

            // 顺序播放
            player.Reset();
            int count = 0;
            while (!player.IsFinished)
            {
                var next = player.GetNext();
                if (next.HasValue) count++;
            }
            TestRunner.AssertEqual(20, count, "sequential play: 20 frames");

            // 清理
            File.Delete(path);
        }

        static void FileHeader()
        {
            TestRunner.StartSection("File Header");

            var metadata = new ReplayMetadata
            {
                GameId = "MyGame",
                GameVersion = 3,
                FrameRate = 60,
                PlayerCount = 4,
                StartTime = new DateTime(2026, 1, 1),
            };

            string path = Path.Combine(Path.GetTempPath(), "test_header.lsrp");
            var recorder = new ReplayRecorder(_serializer, metadata);
            recorder.Start(path);
            recorder.Record(new FramePackage(1, new DeterministicInput[0]));
            recorder.Stop();

            var player = new ReplayPlayer(_serializer);
            player.Load(path);

            TestRunner.AssertEqual("MyGame", player.Metadata.GameId, "game id preserved");
            TestRunner.AssertEqual(3, player.Metadata.GameVersion, "game version preserved");
            TestRunner.AssertEqual(60, player.Metadata.FrameRate, "frame rate preserved");
            TestRunner.AssertEqual(4, player.Metadata.PlayerCount, "player count preserved");

            File.Delete(path);
        }
    }
}
