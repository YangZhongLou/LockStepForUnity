using System;
using System.Collections.Generic;

namespace LockStepLib.Core
{
    /// <summary>
    /// 字节数组池。按 2 的幂分级 (256B ~ 64KB)，减少网络收发中的 GC 分配。
    /// 非线程安全 — 仅在主线程使用（帧同步所有操作都在主线程）。
    /// </summary>
    public static class ByteArrayPool
    {
        /// <summary>最小池化大小 (256 B)</summary>
        private const int MIN_POWER = 8;  // 2^8 = 256

        /// <summary>最大池化大小 (64 KB)</summary>
        private const int MAX_POWER = 16; // 2^16 = 65536

        /// <summary>各级池，key = 2 的幂指数</summary>
        private static readonly Dictionary<int, Stack<byte[]>> Pools = new Dictionary<int, Stack<byte[]>>();

        static ByteArrayPool()
        {
            for (int p = MIN_POWER; p <= MAX_POWER; p++)
                Pools[p] = new Stack<byte[]>();
        }

        /// <summary>租借不小于 minSize 的最小池化字节数组</summary>
        public static byte[] Rent(int minSize)
        {
            if (minSize <= 0) return Array.Empty<byte>();

            int power = CeilPowerOfTwoExponent(minSize);
            if (power < MIN_POWER) power = MIN_POWER;
            if (power > MAX_POWER) return new byte[minSize]; // 超大请求直接分配

            var pool = Pools[power];
            return pool.Count > 0 ? pool.Pop() : new byte[1 << power];
        }

        /// <summary>归还数组到池中</summary>
        public static void Return(byte[] array)
        {
            if (array == null || array.Length == 0) return;

            int size = array.Length;
            int power = ExactPowerOfTwo(size);
            if (power < MIN_POWER || power > MAX_POWER || (1 << power) != size)
                return; // 不符合池化规格，丢弃

            Array.Clear(array, 0, array.Length);
            Pools[power].Push(array);
        }

        /// <summary>池化包装结构，using 自动归还 (不可重复 Dispose)</summary>
        public struct RentedArray : IDisposable
        {
            public byte[] Array { get; private set; }
            private bool _disposed;

            public RentedArray(int minSize)
            {
                Array = Rent(minSize);
                _disposed = false;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (Array != null) { Return(Array); Array = null; }
            }
        }

        #region 辅助

        private static int CeilPowerOfTwoExponent(int v)
        {
            int p = 0;
            int n = 1;
            while (n < v) { n <<= 1; p++; }
            return p;
        }

        private static int ExactPowerOfTwo(int v)
        {
            int p = 0;
            while (v > 1) { v >>= 1; p++; }
            return p;
        }

        /// <summary>获取池状态，调试用</summary>
        public static string GetStats()
        {
            var sb = new System.Text.StringBuilder();
            for (int p = MIN_POWER; p <= MAX_POWER; p++)
                sb.AppendLine($"  {1 << p,6} B: {Pools[p].Count} 空闲");
            return sb.ToString();
        }

        #endregion
    }
}
