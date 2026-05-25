using System;
using LockStepLib.Consistency;

namespace Tests
{
    public static class ConsistencyCheckerTests
    {
        public static void Run()
        {
            Console.WriteLine("--- ConsistencyChecker Tests ---");

            CheckFrameMatch();
            CheckFrameMismatch();
            Trim();
        }

        static void CheckFrameMatch()
        {
            TestRunner.StartSection("Hash Match");

            var checker = new ConsistencyChecker(10);
            TestRunner.AssertEqual(true, checker.IsCheckFrame(10), "frame 10 is check frame");
            TestRunner.AssertEqual(false, checker.IsCheckFrame(11), "frame 11 is not check frame");

            checker.RecordLocalHash(10, 0, 0xABCD);
            checker.RecordLocalHash(10, 1, 0xABCD);

            var mismatched = checker.CheckFrame(10);
            TestRunner.AssertEqual(0, mismatched.Length, "no mismatches when hashes match");
            TestRunner.Assert(null == checker.LastDesync, "no desync recorded");
        }

        static void CheckFrameMismatch()
        {
            TestRunner.StartSection("Hash Mismatch");

            var checker = new ConsistencyChecker(10);
            checker.RecordLocalHash(20, 0, 0x1111);
            checker.RecordLocalHash(20, 1, 0x2222);

            var mismatched = checker.CheckFrame(20);
            TestRunner.Assert(mismatched.Length > 0, "mismatch detected");
            TestRunner.Assert(checker.LastDesync != null, "desync info recorded");
            TestRunner.AssertEqual(20, checker.LastDesync.Frame, "desync frame = 20");
        }

        static void Trim()
        {
            TestRunner.StartSection("Trim");

            var checker = new ConsistencyChecker(10);
            checker.RecordLocalHash(10, 0, 0xAAAA);
            checker.RecordLocalHash(50, 0, 0xBBBB);
            checker.RecordLocalHash(100, 0, 0xCCCC);

            checker.Trim(50);
            TestRunner.AssertEqual(0, checker.CheckFrame(10).Length, "frame 10 data trimmed"); // returns empty array
        }
    }
}
