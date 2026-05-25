using System;
using System.IO;
using LockStepLib.Command;

namespace Tests
{
    public static class CommandBufferTests
    {
        // 简单空指令
        class NoopCmd : IInputCommand
        {
            public int CommandTypeId => -1;
            public void Serialize(BinaryWriter w) { }
            public void Deserialize(BinaryReader r) { }
        }

        public static void Run()
        {
            Console.WriteLine("--- CommandBuffer Tests ---");

            AddAndDedupe();
            AllPlayersReady();
            GetInputsForFrame();
            LatestFrame();
            Trim();
            AddFramePackage();
        }

        static void AddAndDedupe()
        {
            TestRunner.StartSection("Add & Dedupe");

            var buf = new CommandBuffer();
            var cmd = new NoopCmd();

            bool a1 = buf.AddInput(0, 10, cmd);
            bool a2 = buf.AddInput(0, 10, cmd); // duplicate
            bool a3 = buf.AddInput(1, 10, cmd);

            TestRunner.AssertEqual(true, a1, "first add → true");
            TestRunner.AssertEqual(false, a2, "duplicate → false");
            TestRunner.AssertEqual(true, a3, "different player → true");
            TestRunner.AssertEqual(2, buf.Count, "2 entries");
        }

        static void AllPlayersReady()
        {
            TestRunner.StartSection("AllPlayersReady");

            var buf = new CommandBuffer();
            buf.AddInput(0, 5, new NoopCmd());
            buf.AddInput(1, 5, new NoopCmd());

            TestRunner.AssertEqual(true, buf.AllPlayersReady(5, 2), "2 players all ready");
            TestRunner.AssertEqual(false, buf.AllPlayersReady(5, 3), "3 players, only 2 ready → false");
            TestRunner.AssertEqual(false, buf.AllPlayersReady(10, 2), "frame 10 empty → false");
        }

        static void GetInputsForFrame()
        {
            TestRunner.StartSection("GetInputsForFrame");

            var buf = new CommandBuffer();
            var cmd = new NoopCmd();
            buf.AddInput(0, 20, cmd);
            buf.AddInput(1, 20, cmd);

            var inputs = buf.GetInputsForFrame(20, 2);
            TestRunner.AssertEqual(2, inputs.Length, "2 inputs returned");
            TestRunner.AssertEqual(0, inputs[0].PlayerId, "first P0");
            TestRunner.AssertEqual(1, inputs[1].PlayerId, "second P1");

            // partial: only 1 of 3 players submitted
            var partial = buf.GetInputsForFrame(20, 3);
            TestRunner.AssertEqual(2, partial.Length, "only 2 of 3");
        }

        static void LatestFrame()
        {
            TestRunner.StartSection("LatestFrame");

            var buf = new CommandBuffer();
            buf.AddInput(0, 5, new NoopCmd());
            buf.AddInput(0, 10, new NoopCmd());
            buf.AddInput(0, 3, new NoopCmd());

            TestRunner.AssertEqual(10, buf.GetLatestFrameForPlayer(0), "latest for P0 = 10");
            TestRunner.AssertEqual(-1, buf.GetLatestFrameForPlayer(1), "P1 never submitted → -1");
        }

        static void Trim()
        {
            TestRunner.StartSection("Trim");

            var buf = new CommandBuffer();
            buf.AddInput(0, 10, new NoopCmd());
            buf.AddInput(0, 50, new NoopCmd());
            buf.AddInput(0, 100, new NoopCmd());

            TestRunner.AssertEqual(3, buf.Count, "before trim: 3");

            buf.Trim(50);
            TestRunner.AssertEqual(2, buf.Count, "after trim(50): 2");
            TestRunner.AssertEqual(false, buf.HasInput(0, 10), "F10 removed");
            TestRunner.AssertEqual(true, buf.HasInput(0, 50), "F50 kept");
            TestRunner.AssertEqual(true, buf.HasInput(0, 100), "F100 kept");
        }

        static void AddFramePackage()
        {
            TestRunner.StartSection("AddFramePackage");

            var buf = new CommandBuffer();
            buf.AddInput(0, 1, new NoopCmd()); // pre-existing

            var pkg = new FramePackage(
                frame: 2,
                primary: new[] {
                    new DeterministicInput(0, 2, new NoopCmd()),
                    new DeterministicInput(1, 2, new NoopCmd()),
                },
                redundancy: new[] {
                    new DeterministicInput(0, 1, new NoopCmd()), // should dedupe
                }
            );

            int accepted = buf.AddFramePackage(pkg);
            TestRunner.AssertEqual(2, accepted, "2 new (1 deduped)");
            TestRunner.AssertEqual(3, buf.Count, "total 3 entries");
        }
    }
}
