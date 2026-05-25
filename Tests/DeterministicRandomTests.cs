using System;
using LockStepLib.Math;
using LockStepLib.Simulation;

namespace Tests
{
    public static class DeterministicRandomTests
    {
        public static void Run()
        {
            Console.WriteLine("--- DeterministicRandom Tests ---");

            SameSeedSameSequence();
            Reset();
            RangeDistribution();
            NextFix64();
        }

        static void SameSeedSameSequence()
        {
            TestRunner.StartSection("Same Seed = Same Sequence");

            var r1 = new DeterministicRandom(42UL);
            var r2 = new DeterministicRandom(42UL);

            bool allSame = true;
            for (int i = 0; i < 100; i++)
            {
                if (r1.Next() != r2.Next()) { allSame = false; break; }
            }
            TestRunner.Assert(allSame, "100 calls, all identical with seed 42");
        }

        static void Reset()
        {
            TestRunner.StartSection("Reset");

            var r = new DeterministicRandom(99UL);
            var first10 = new int[10];
            for (int i = 0; i < 10; i++) first10[i] = r.Next();

            r.Reset(99UL);
            var second10 = new int[10];
            for (int i = 0; i < 10; i++) second10[i] = r.Next();

            bool allMatch = true;
            for (int i = 0; i < 10; i++)
                if (first10[i] != second10[i]) { allMatch = false; break; }
            TestRunner.Assert(allMatch, "reset seed 99 → same sequence");
        }

        static void RangeDistribution()
        {
            TestRunner.StartSection("Range Distribution");

            var r = new DeterministicRandom(777UL);
            int[] buckets = new int[10];

            for (int i = 0; i < 10000; i++)
                buckets[r.Next(10)]++;

            // 期望每桶约 1000
            int min = int.MaxValue, max = int.MinValue;
            foreach (int b in buckets)
            {
                if (b < min) min = b;
                if (b > max) max = b;
            }

            TestRunner.Assert(min > 850, $"min bucket {min} > 850");
            TestRunner.Assert(max < 1150, $"max bucket {max} < 1150");
        }

        static void NextFix64()
        {
            TestRunner.StartSection("NextFix64");

            var r = new DeterministicRandom(123UL);

            // 100 次调用全部在 [0, 1) 内
            bool allInRange = true;
            for (int i = 0; i < 100; i++)
            {
                var v = r.NextFix64();
                if (v.RawValue < 0 || v.RawValue >= Fix64.ONE_I)
                    allInRange = false;
            }
            TestRunner.Assert(allInRange, "100 values all in [0, 1)");

            // 范围随机在 [min, max] 内
            var minVal = Fix64.FromInt(5);
            var maxVal = Fix64.FromInt(10);
            allInRange = true;
            for (int i = 0; i < 100; i++)
            {
                var v = r.NextFix64(minVal, maxVal);
                if (v.RawValue < minVal.RawValue || v.RawValue > maxVal.RawValue)
                    allInRange = false;
            }
            TestRunner.Assert(allInRange, "100 values all in [5, 10]");
        }
    }
}
