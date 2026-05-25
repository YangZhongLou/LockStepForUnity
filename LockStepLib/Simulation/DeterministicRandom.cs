using LockStepLib.Math;

namespace LockStepLib.Simulation
{
    /// <summary>
    /// 确定性伪随机数生成器。使用 xorshift128+ 算法。
    /// 相同种子始终生成相同序列，适合帧同步中的随机逻辑。
    /// </summary>
    public class DeterministicRandom
    {
        private ulong _state0;
        private ulong _state1;

        /// <summary>当前种子 (state0)</summary>
        public ulong Seed => _state0;

        public DeterministicRandom() : this(123456789UL) { }

        /// <summary>使用指定种子初始化</summary>
        public DeterministicRandom(ulong seed)
        {
            _state0 = seed;
            _state1 = seed ^ 0xDEADBEEFCAFEBABEUL;
            // 预热
            for (int i = 0; i < 10; i++) NextRaw();
        }

        /// <summary>重置到指定种子</summary>
        public void Reset(ulong seed)
        {
            _state0 = seed;
            _state1 = seed ^ 0xDEADBEEFCAFEBABEUL;
            for (int i = 0; i < 10; i++) NextRaw();
        }

        #region 核心算法

        /// <summary>xorshift128+ 原始输出</summary>
        private ulong NextRaw()
        {
            ulong s1 = _state0;
            ulong s0 = _state1;
            _state0 = s0;
            s1 ^= s1 << 23;
            _state1 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5);
            return _state1 + s0;
        }

        #endregion

        #region 整数随机

        /// <summary>返回 [0, int.MaxValue] 范围内的随机 int</summary>
        public int Next()
        {
            return (int)(NextRaw() & 0x7FFFFFFF);
        }

        /// <summary>返回 [0, maxValue) 范围内的随机 int</summary>
        public int Next(int maxValue)
        {
            if (maxValue <= 0) return 0;
            return (int)(NextRaw() % (ulong)maxValue);
        }

        /// <summary>返回 [minValue, maxValue) 范围内的随机 int</summary>
        public int Next(int minValue, int maxValue)
        {
            if (minValue >= maxValue) return minValue;
            return minValue + Next(maxValue - minValue);
        }

        #endregion

        #region Fix64 随机

        /// <summary>返回 [0, 1] 范围内的 Fix64 随机数 (含 0，不含 1)</summary>
        public Fix64 NextFix64()
        {
            // 使用低 16 位作为 Q16.16 小数部分
            uint frac = (uint)(NextRaw() & 0xFFFF);
            return Fix64.FromRaw((int)frac);
        }

        /// <summary>返回 [min, max] 范围内的 Fix64 随机数</summary>
        public Fix64 NextFix64(Fix64 min, Fix64 max)
        {
            if (max.RawValue <= min.RawValue) return min;
            Fix64 range = max - min;
            uint frac = (uint)(NextRaw() & 0xFFFF);
            Fix64 t = Fix64.FromRaw((int)frac);
            return min + range * t;
        }

        #endregion
    }
}
