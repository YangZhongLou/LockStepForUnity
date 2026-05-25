using System;
using LockStepLib.Core;

namespace Tests
{
    public static class VarIntTests
    {
        public static void Run()
        {
            Console.WriteLine("--- VarInt Tests ---");

            UInt32Roundtrip();
            Int32Roundtrip();
            Lengths();
        }

        static void UInt32Roundtrip()
        {
            TestRunner.StartSection("UInt32 Roundtrip");

            TestU32(0u, 1);
            TestU32(127u, 1);
            TestU32(128u, 2);
            TestU32(16383u, 2);
            TestU32(2097151u, 3);
            TestU32(uint.MaxValue, 5);
        }

        static void Int32Roundtrip()
        {
            TestRunner.StartSection("Int32 Roundtrip (ZigZag)");

            TestI32(0, 1);
            TestI32(-1, 1);
            TestI32(1, 1);
            TestI32(int.MaxValue, 5);
            TestI32(int.MinValue, 5);
            TestI32(-128, 2);
            TestI32(128, 2);
        }

        static void Lengths()
        {
            TestRunner.StartSection("Length Query");

            TestRunner.AssertEqual(1, VarInt.GetUInt32Length(0), "len(0)=1");
            TestRunner.AssertEqual(1, VarInt.GetUInt32Length(127), "len(127)=1");
            TestRunner.AssertEqual(2, VarInt.GetUInt32Length(128), "len(128)=2");
            TestRunner.AssertEqual(2, VarInt.GetUInt32Length(16383), "len(16383)=2");
            TestRunner.AssertEqual(4, VarInt.GetUInt32Length(2097152), "len(2097152)=4");
        }

        static void TestU32(uint value, int expectedBytes)
        {
            byte[] buf = new byte[10];
            int written = VarInt.WriteUInt32(buf, 0, value);
            int offset = 0;
            uint decoded = VarInt.ReadUInt32(buf, ref offset);

            string label = $"{value}";
            TestRunner.AssertEqual(expectedBytes, written, $"U32 {label}: bytes={expectedBytes}");
            TestRunner.AssertEqual((int)value, (int)decoded, $"U32 {label}: value match");
        }

        static void TestI32(int value, int expectedBytes)
        {
            byte[] buf = new byte[10];
            int written = VarInt.WriteInt32(buf, 0, value);
            int offset = 0;
            int decoded = VarInt.ReadInt32(buf, ref offset);

            string label = $"{value}";
            TestRunner.AssertEqual(expectedBytes, written, $"I32 {label}: bytes={expectedBytes}");
            TestRunner.AssertEqual(value, decoded, $"I32 {label}: value match");
        }
    }
}
