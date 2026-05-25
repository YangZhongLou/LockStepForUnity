using System;
using LockStepLib.Core;

namespace Tests
{
    public static class ByteArrayPoolTests
    {
        public static void Run()
        {
            Console.WriteLine("--- ByteArrayPool Tests ---");

            RentSizes();
            Reuse();
            Oversized();
            DoubleDispose();
        }

        static void RentSizes()
        {
            TestRunner.StartSection("Rent Sizes");

            var a = ByteArrayPool.Rent(100);
            TestRunner.AssertEqual(256, a.Length, "Rent(100) → 256");
            ByteArrayPool.Return(a);

            var b = ByteArrayPool.Rent(300);
            TestRunner.AssertEqual(512, b.Length, "Rent(300) → 512");
            ByteArrayPool.Return(b);

            var c = ByteArrayPool.Rent(1024);
            TestRunner.AssertEqual(1024, c.Length, "Rent(1024) → 1024");
            ByteArrayPool.Return(c);

            var d = ByteArrayPool.Rent(2000);
            TestRunner.AssertEqual(2048, d.Length, "Rent(2000) → 2048");
            ByteArrayPool.Return(d);
        }

        static void Reuse()
        {
            TestRunner.StartSection("Reuse");

            var first = ByteArrayPool.Rent(256);
            ByteArrayPool.Return(first);
            var second = ByteArrayPool.Rent(256);

            TestRunner.AssertEqual(true, ReferenceEquals(first, second), "Same instance after return");

            ByteArrayPool.Return(second);
        }

        static void Oversized()
        {
            TestRunner.StartSection("Oversized");

            // 超过 MAX_POWER (64KB) 的请求直接分配，不入池
            var big = ByteArrayPool.Rent(100000);
            TestRunner.AssertEqual(100000, big.Length, "Rent(100000) → 100000");
            ByteArrayPool.Return(big); // should be discarded silently
        }

        static void DoubleDispose()
        {
            TestRunner.StartSection("Double Dispose");

            var rented = new ByteArrayPool.RentedArray(256);
            var arr = rented.Array;
            rented.Dispose();
            rented.Dispose(); // 不应抛异常，不应重复归还
            TestRunner.Assert(true, "double dispose no throw");
        }
    }
}
