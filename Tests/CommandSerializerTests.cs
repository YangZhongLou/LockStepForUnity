using System;
using System.IO;
using LockStepLib.Command;

namespace Tests
{
    // 测试用指令
    class TestMoveCmd : IInputCommand
    {
        public int CommandTypeId => 100;
        public int X, Y;

        public TestMoveCmd() { }
        public TestMoveCmd(int x, int y) { X = x; Y = y; }

        public void Serialize(BinaryWriter w) { w.Write(X); w.Write(Y); }
        public void Deserialize(BinaryReader r) { X = r.ReadInt32(); Y = r.ReadInt32(); }
    }

    class TestAttackCmd : IInputCommand
    {
        public int CommandTypeId => 200;
        public int TargetId;

        public TestAttackCmd() { }
        public TestAttackCmd(int targetId) { TargetId = targetId; }

        public void Serialize(BinaryWriter w) { w.Write(TargetId); }
        public void Deserialize(BinaryReader r) { TargetId = r.ReadInt32(); }
    }

    public static class CommandSerializerTests
    {
        private static CommandFactory _factory;
        private static CommandSerializer _serializer;

        public static void Run()
        {
            Console.WriteLine("--- CommandSerializer Tests ---");

            _factory = new CommandFactory();
            _factory.Register(100, () => new TestMoveCmd());
            _factory.Register(200, () => new TestAttackCmd());
            _serializer = new CommandSerializer(_factory);

            SingleInputRoundtrip();
            BatchRoundtrip();
            FramePackageRoundtrip();
            EmptyPackage();
        }

        static void SingleInputRoundtrip()
        {
            TestRunner.StartSection("Single Input Roundtrip");

            var cmd = new TestMoveCmd(10, 20);
            var input = new DeterministicInput(0, 42, cmd);

            var data = _serializer.SerializeInput(input);
            var back = _serializer.DeserializeInput(data);

            TestRunner.AssertEqual(0, back.PlayerId, "PlayerId=0");
            TestRunner.AssertEqual(42, back.Frame, "Frame=42");
            TestRunner.AssertEqual(true, back.Command is TestMoveCmd, "Type=TestMoveCmd");
            var mc = (TestMoveCmd)back.Command;
            TestRunner.AssertEqual(10, mc.X, "X=10");
            TestRunner.AssertEqual(20, mc.Y, "Y=20");
        }

        static void BatchRoundtrip()
        {
            TestRunner.StartSection("Batch Roundtrip");

            var inputs = new DeterministicInput[]
            {
                new DeterministicInput(0, 100, new TestMoveCmd(1, 2)),
                new DeterministicInput(1, 100, new TestAttackCmd(99)),
            };

            var data = _serializer.SerializeInputs(inputs);
            var back = _serializer.DeserializeInputs(data);

            TestRunner.AssertEqual(2, back.Length, "count=2");
            TestRunner.AssertEqual(0, back[0].PlayerId, "P0");
            TestRunner.AssertEqual(1, back[1].PlayerId, "P1");
            TestRunner.AssertEqual(true, back[1].Command is TestAttackCmd, "P1's cmd is Attack");
        }

        static void FramePackageRoundtrip()
        {
            TestRunner.StartSection("FramePackage Roundtrip");

            var pkg = new FramePackage(
                frame: 50,
                primary: new[] { new DeterministicInput(0, 50, new TestMoveCmd(3, 4)) },
                redundancy: new[] { new DeterministicInput(0, 49, new TestMoveCmd(1, 1)) },
                consistency: new ConsistencyData(40, 0xCAFE_BABE_DEAD_BEEF)
            );

            var data = _serializer.SerializeFramePackage(pkg);
            var back = _serializer.DeserializeFramePackage(data);

            TestRunner.AssertEqual(50, back.Frame, "Frame=50");
            TestRunner.AssertEqual(1, back.PrimaryInputs.Length, "Primary count=1");
            TestRunner.AssertEqual(1, back.RedundancyInputs?.Length ?? 0, "Redundancy count=1");
            TestRunner.AssertEqual(true, back.Consistency.HasValue, "Has consistency");
            TestRunner.AssertEqual(40, back.Consistency.Value.CheckFrame, "CheckFrame=40");
            TestRunner.AssertEqual(0xCAFE_BABE_DEAD_BEEF, back.Consistency.Value.FrameHash, "Hash match");
        }

        static void EmptyPackage()
        {
            TestRunner.StartSection("Empty Package");

            var pkg = new FramePackage(0, new DeterministicInput[0]);
            var data = _serializer.SerializeFramePackage(pkg);
            var back = _serializer.DeserializeFramePackage(data);

            TestRunner.AssertEqual(0, back.Frame, "Frame=0");
            TestRunner.AssertEqual(0, back.PrimaryInputs.Length, "Primary empty");
            TestRunner.Assert(back.RedundancyInputs == null, "Redundancy null");
            TestRunner.AssertEqual(false, back.Consistency.HasValue, "No consistency");
        }
    }
}
