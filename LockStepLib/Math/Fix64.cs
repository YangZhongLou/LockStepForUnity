using System;

namespace LockStepLib.Math
{
    /// <summary>
    /// Q16.16 定点数。使用 32 位有符号整数存储，16 位整数部分 + 16 位小数部分。
    /// 范围约 ±32767.99998，精度约 0.000015。
    /// 所有运算确定性 —— 不依赖 System.Math 运行时实现。
    /// </summary>
    public readonly struct Fix64 : IEquatable<Fix64>, IComparable<Fix64>
    {
        /// <summary>内部原始值。x = RawValue / 2^16</summary>
        public readonly int RawValue;

        private const int FRAC_BITS = 16;
        private const long ONE_L = 1L << FRAC_BITS;            // 65536
        internal const int ONE_I = (int)ONE_L;                 // 65536
        private const long HALF_L = ONE_L >> 1;                // 32768

        #region 常量

        public static readonly Fix64 Zero = new Fix64(0);
        public static readonly Fix64 One = new Fix64(ONE_I);
        public static readonly Fix64 NegativeOne = new Fix64(-ONE_I);
        public static readonly Fix64 Half = new Fix64((int)HALF_L);
        public static readonly Fix64 Quarter = new Fix64(ONE_I / 4);

        /// <summary>可表示的最小正值 (1 / 2^16)</summary>
        public static readonly Fix64 Epsilon = new Fix64(1);

        public static readonly Fix64 Pi = new Fix64(205887);              // 3.14159... * 65536
        public static readonly Fix64 PiOver2 = new Fix64(102944);         // PI / 2
        public static readonly Fix64 TwoPi = new Fix64(411775);           // 2 * PI
        public static readonly Fix64 E = new Fix64(178145);               // 2.71828... * 65536

        // 常用三角函数值
        public static readonly Fix64 Deg2Rad = Pi / new Fix64(180 * ONE_I);
        public static readonly Fix64 Rad2Deg = new Fix64(180 * ONE_I) / Pi;

        public static readonly Fix64 MaxValue = new Fix64(int.MaxValue);
        public static readonly Fix64 MinValue = new Fix64(int.MinValue);

        #endregion

        internal Fix64(int raw)
        {
            RawValue = raw;
        }

        #region 构造方法

        public static Fix64 FromRaw(int raw) => new Fix64(raw);

        public static Fix64 FromInt(int value)
        {
            long v = (long)value << FRAC_BITS;
            return new Fix64((int)Clamp64(v));
        }

        public static Fix64 FromFloat(float value)
        {
            long v = (long)(value * ONE_L + (value >= 0 ? 0.5f : -0.5f));
            return new Fix64((int)Clamp64(v));
        }

        public static Fix64 FromDouble(double value)
        {
            long v = (long)(value * ONE_L + (value >= 0 ? 0.5 : -0.5));
            return new Fix64((int)Clamp64(v));
        }

        #endregion

        #region 类型转换

        public double ToDouble() => (double)RawValue / ONE_L;

        public float ToFloat() => (float)RawValue / ONE_I;

        public int ToInt() => RawValue >> FRAC_BITS;

        /// <summary>向下取整</summary>
        public int FloorToInt() => RawValue >= 0 ? RawValue >> FRAC_BITS : (RawValue - ONE_I + 1) >> FRAC_BITS;

        /// <summary>向上取整</summary>
        public int CeilToInt() => RawValue >= 0 ? (RawValue + ONE_I - 1) >> FRAC_BITS : RawValue >> FRAC_BITS;

        #endregion

        #region 算术运算符

        public static Fix64 operator +(Fix64 a, Fix64 b) => new Fix64(a.RawValue + b.RawValue);

        public static Fix64 operator -(Fix64 a, Fix64 b) => new Fix64(a.RawValue - b.RawValue);

        public static Fix64 operator -(Fix64 a) => new Fix64(-a.RawValue);

        public static Fix64 operator *(Fix64 a, Fix64 b)
        {
            long v = (long)a.RawValue * b.RawValue;
            return new Fix64((int)Clamp64(v >> FRAC_BITS));
        }

        public static Fix64 operator /(Fix64 a, Fix64 b)
        {
            long v = ((long)a.RawValue << FRAC_BITS) / b.RawValue;
            return new Fix64((int)Clamp64(v));
        }

        #endregion

        #region 比较运算符

        public static bool operator ==(Fix64 a, Fix64 b) => a.RawValue == b.RawValue;
        public static bool operator !=(Fix64 a, Fix64 b) => a.RawValue != b.RawValue;
        public static bool operator >(Fix64 a, Fix64 b) => a.RawValue > b.RawValue;
        public static bool operator <(Fix64 a, Fix64 b) => a.RawValue < b.RawValue;
        public static bool operator >=(Fix64 a, Fix64 b) => a.RawValue >= b.RawValue;
        public static bool operator <=(Fix64 a, Fix64 b) => a.RawValue <= b.RawValue;

        #endregion

        #region 数学方法

        public static Fix64 Abs(Fix64 x) => x.RawValue < 0 ? new Fix64(x.RawValue == int.MinValue ? int.MaxValue : -x.RawValue) : x;

        public static int Sign(Fix64 x) => x.RawValue > 0 ? 1 : x.RawValue < 0 ? -1 : 0;

        /// <summary>向下取整，返回 Fix64</summary>
        public static Fix64 Floor(Fix64 x)
        {
            int r = x.RawValue;
            return new Fix64(r & ~(ONE_I - 1));
        }

        /// <summary>向上取整，返回 Fix64</summary>
        public static Fix64 Ceil(Fix64 x)
        {
            int r = x.RawValue;
            return new Fix64((r + ONE_I - 1) & ~(ONE_I - 1));
        }

        /// <summary>四舍五入 (half-away-from-zero)</summary>
        public static Fix64 Round(Fix64 x)
        {
            int r = x.RawValue;
            int frac = r & (ONE_I - 1); // 始终在 [0, 65535], 表示 floor 到 r 的小数部分
            int floor = r & ~(ONE_I - 1);
            if (frac > HALF_L)
                return new Fix64(floor + ONE_I);
            if (frac == HALF_L)
                return new Fix64(r >= 0 ? floor + ONE_I : floor); // half-away-from-zero
            return new Fix64(floor);
        }

        public static Fix64 Min(Fix64 a, Fix64 b) => a.RawValue <= b.RawValue ? a : b;

        public static Fix64 Max(Fix64 a, Fix64 b) => a.RawValue >= b.RawValue ? a : b;

        public static Fix64 Clamp(Fix64 value, Fix64 min, Fix64 max)
        {
            if (value.RawValue < min.RawValue) return min;
            if (value.RawValue > max.RawValue) return max;
            return value;
        }

        public static Fix64 Lerp(Fix64 a, Fix64 b, Fix64 t)
        {
            // a + (b - a) * t  (clamped t to [0, 1])
            if (t.RawValue <= 0) return a;
            if (t.RawValue >= ONE_I) return b;
            return a + (b - a) * t;
        }

        /// <summary>
        /// 牛顿迭代法求平方根。在 Q16.16 域内执行，固定迭代次数以保证确定性。
        /// </summary>
        public static Fix64 Sqrt(Fix64 x)
        {
            if (x.RawValue <= 0) return Zero;

            int raw = x.RawValue;

            // 初始猜测 (Q16.16): 1 << ((msb(x_raw) + 16) / 2)
            long guess;
            if (raw >= ONE_I)
            {
                int msb = MostSignificantBit((ulong)raw);
                guess = 1L << ((msb + FRAC_BITS) / 2);
            }
            else
            {
                // x < 1: 使用 x_raw 作为低估猜测
                guess = raw < 1 ? ONE_I : raw;
            }

            // 牛顿迭代: y_{n+1} = (y_n + x / y_n) / 2  (全部 Q16.16)
            const int iterations = 10;
            for (int i = 0; i < iterations; i++)
            {
                long div = ((long)raw << FRAC_BITS) / guess; // (x << 16) / guess, 结果在 Q16.16
                guess = (guess + div) >> 1;
            }

            return new Fix64((int)Clamp64(guess));
        }

        #endregion

        #region 辅助方法

        private static long Clamp64(long v)
        {
            if (v > int.MaxValue) return int.MaxValue;
            if (v < int.MinValue) return int.MinValue;
            return v;
        }

        /// <summary>获取 ulong 值的最高有效位位置 (0-based)</summary>
        private static int MostSignificantBit(ulong v)
        {
            int bit = 0;
            while (v > 0) { v >>= 1; bit++; }
            return bit;
        }

        #endregion

        #region IEquatable / IComparable / Object

        public bool Equals(Fix64 other) => RawValue == other.RawValue;

        public int CompareTo(Fix64 other) => RawValue.CompareTo(other.RawValue);

        public override bool Equals(object obj) => obj is Fix64 f && Equals(f);

        public override int GetHashCode() => RawValue;

        public override string ToString() => ToDouble().ToString("F6");

        /// <summary>以原始十六进制值显示，调试用</summary>
        public string ToRawString() => $"0x{RawValue:X8} ({ToDouble():F6})";

        #endregion

        #region 显式类型转换运算符 (方便与 float/double 互操作，仅用于非确定性代码)

        public static explicit operator float(Fix64 v) => v.ToFloat();
        public static explicit operator double(Fix64 v) => v.ToDouble();

        #endregion
    }
}
