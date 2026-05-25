using System;
using System.Diagnostics;
using LockStepLib.Math;
using LockStepLib.Core;
using LockStepLib.Command;
using System.IO;

namespace Tests
{
    // 性能测试用空指令
    class PerfCmd : IInputCommand
    {
        public int CommandTypeId => 0;
        public void Serialize(BinaryWriter w) { }
        public void Deserialize(BinaryReader r) { }
    }

    /// <summary>
    /// 性能基准测试。不验证正确性，只输出耗时供评估。
    /// </summary>
    public static class PerfTests
    {
        private const int WARMUP = 1000;
        private const int ITERATIONS = 100000;

        public static void Run()
        {
            Console.WriteLine("--- Performance Benchmarks ---\n");

            Fix64Arithmetic();
            FixMathTrig();
            VarIntEncoding();
            CommandBufferOps();
            ByteArrayPoolAlloc();
        }

        static void Fix64Arithmetic()
        {
            Console.WriteLine("  Fix64 Arithmetic (100K ops):");
            var a = Fix64.FromDouble(3.14159);
            var b = Fix64.FromDouble(2.71828);

            var sw = Stopwatch.StartNew();
            Fix64 sum = Fix64.Zero;
            for (int i = 0; i < ITERATIONS; i++)
                sum = a + b;
            sw.Stop();
            Console.WriteLine($"    Add:     {sw.Elapsed.TotalMilliseconds:F2} ms");

            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
                sum = a * b;
            sw.Stop();
            Console.WriteLine($"    Mul:     {sw.Elapsed.TotalMilliseconds:F2} ms");

            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
                sum = a / b;
            sw.Stop();
            Console.WriteLine($"    Div:     {sw.Elapsed.TotalMilliseconds:F2} ms");

            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
                sum = Fix64.Sqrt(a);
            sw.Stop();
            Console.WriteLine($"    Sqrt:    {sw.Elapsed.TotalMilliseconds:F2} ms");

            // double baseline
            double da = 3.14159, db = 2.71828, dsum;
            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
                dsum = da + db;
            sw.Stop();
            Console.WriteLine($"    [double] Add:  {sw.Elapsed.TotalMilliseconds:F2} ms");
        }

        static void FixMathTrig()
        {
            Console.WriteLine("\n  FixMath Trig (100K ops):");
            var angle = FixMath.PI / Fix64.FromInt(4);

            var sw = Stopwatch.StartNew();
            Fix64 result = Fix64.Zero;
            for (int i = 0; i < ITERATIONS; i++)
                result = FixMath.Sin(angle);
            sw.Stop();
            Console.WriteLine($"    Sin:     {sw.Elapsed.TotalMilliseconds:F2} ms");

            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
                result = FixMath.Cos(angle);
            sw.Stop();
            Console.WriteLine($"    Cos:     {sw.Elapsed.TotalMilliseconds:F2} ms");

            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
                result = FixMath.Atan2(Fix64.One, Fix64.FromInt(2));
            sw.Stop();
            Console.WriteLine($"    Atan2:   {sw.Elapsed.TotalMilliseconds:F2} ms");
        }

        static void VarIntEncoding()
        {
            Console.WriteLine("\n  VarInt Encode/Decode (100K ops):");
            var buf = new byte[10];
            uint[] testValues = { 0, 127, 128, 16383, 2097151, 268435455 };

            var sw = Stopwatch.StartNew();
            for (int j = 0; j < ITERATIONS; j++)
            {
                foreach (var v in testValues)
                    VarInt.WriteUInt32(buf, 0, v);
            }
            sw.Stop();
            Console.WriteLine($"    Encode:  {sw.Elapsed.TotalMilliseconds:F2} ms");

            // prepare data for decode
            var encoded = new byte[testValues.Length][];
            for (int i = 0; i < testValues.Length; i++)
            {
                encoded[i] = new byte[10];
                VarInt.WriteUInt32(encoded[i], 0, testValues[i]);
            }

            sw.Restart();
            for (int j = 0; j < ITERATIONS; j++)
            {
                foreach (var e in encoded)
                {
                    int offset = 0;
                    VarInt.ReadUInt32(e, ref offset);
                }
            }
            sw.Stop();
            Console.WriteLine($"    Decode:  {sw.Elapsed.TotalMilliseconds:F2} ms");
        }

        static void CommandBufferOps()
        {
            Console.WriteLine("\n  CommandBuffer (100K inserts + 100K lookups):");

            var buf = new CommandBuffer();
            var cmd = new PerfCmd();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < ITERATIONS; i++)
                buf.AddInput(i % 4, i, cmd);
            sw.Stop();
            Console.WriteLine($"    Insert:  {sw.Elapsed.TotalMilliseconds:F2} ms");

            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
                buf.HasInput(i % 4, i);
            sw.Stop();
            Console.WriteLine($"    HasInput:{sw.Elapsed.TotalMilliseconds:F2} ms");
        }

        static void ByteArrayPoolAlloc()
        {
            Console.WriteLine("\n  ByteArrayPool (100K alloc/free):");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < ITERATIONS; i++)
            {
                var arr = ByteArrayPool.Rent(512);
                ByteArrayPool.Return(arr);
            }
            sw.Stop();
            Console.WriteLine($"    Rent/Return(512B): {sw.Elapsed.TotalMilliseconds:F2} ms");

            // baseline: new byte[]
            sw.Restart();
            for (int i = 0; i < ITERATIONS; i++)
            {
                var arr = new byte[512];
                // no return (GC collects)
            }
            sw.Stop();
            Console.WriteLine($"    [baseline] new byte[512]: {sw.Elapsed.TotalMilliseconds:F2} ms");
        }
    }
}
