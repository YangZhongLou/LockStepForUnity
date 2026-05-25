using System;

namespace LockStepLib.Math
{
    /// <summary>
    /// 确定性数学函数库。三角函数使用 4096 条目查表 + 线性插值，
    /// 确保跨平台完全一致。所有输入输出均为 Fix64。
    /// </summary>
    public static class FixMath
    {
        /// <summary>正弦表条目数 (覆盖 [0, PI/2])</summary>
        private const int TABLE_SIZE = 4096;

        /// <summary>正弦表，TABLE_SIZE + 1 条 (末尾用于插值边界)</summary>
        private static readonly int[] SinTable;

        /// <summary>sin_table[i] = sin(i * PI/2 / TABLE_SIZE) * 2^16</summary>
        private static readonly int[] AsinIndexTable; // 反向映射: 对每个 raw 值记录最近索引

        /// <summary>缩放因子: TABLE_SIZE * 2 / PI (Q16.16)，用于角度 → 索引</summary>
        private const int SCALE_FACTOR_RAW = 170893398; // (8192 / PI) * 65536

        /// <summary>PI * 65536</summary>
        private const int PI_RAW = 205887;

        /// <summary>PI/2 * 65536</summary>
        private const int PI_OVER_2_RAW = 102944;

        /// <summary>2PI * 65536</summary>
        private const int TWO_PI_RAW = 411775;

        #region 公共常量

        public static readonly Fix64 PI = new Fix64(PI_RAW);
        public static readonly Fix64 PiOver2 = new Fix64(PI_OVER_2_RAW);
        public static readonly Fix64 TwoPi = new Fix64(TWO_PI_RAW);
        public static readonly Fix64 E = Fix64.E;
        public static readonly Fix64 Deg2Rad = PI / new Fix64(180 * Fix64.ONE_I);
        public static readonly Fix64 Rad2Deg = new Fix64(180 * Fix64.ONE_I) / PI;

        #endregion

        static FixMath()
        {
            // 生成正弦表
            SinTable = new int[TABLE_SIZE + 1];
            for (int i = 0; i <= TABLE_SIZE; i++)
            {
                double angle = (double)i / TABLE_SIZE * (System.Math.PI / 2.0);
                double s = System.Math.Sin(angle);
                SinTable[i] = (int)(s * 65536.0 + 0.5);
            }
            // 确保边界精确
            SinTable[0] = 0;
            SinTable[TABLE_SIZE] = 65536;

            // 生成反正弦反向索引表 (用于加速 Asin 二分查找)
            AsinIndexTable = new int[65537]; // 索引 0..65536
            int tableIdx = 0;
            for (int raw = 0; raw <= 65536; raw++)
            {
                while (tableIdx < TABLE_SIZE && SinTable[tableIdx + 1] <= raw)
                    tableIdx++;
                AsinIndexTable[raw] = tableIdx;
            }
        }

        #region 角度归一化

        /// <summary>将角度归一化到 [0, 2*PI) 范围</summary>
        private static Fix64 NormalizeAngle(Fix64 angle)
        {
            int raw = angle.RawValue;
            if (raw >= 0 && raw < TWO_PI_RAW) return angle;

            raw %= TWO_PI_RAW;
            if (raw < 0) raw += TWO_PI_RAW;
            return new Fix64(raw);
        }

        #endregion

        #region Sin / Cos / Tan

        /// <summary>正弦函数，确定性查表 + 线性插值</summary>
        public static Fix64 Sin(Fix64 angle)
        {
            int raw = NormalizeAngle(angle).RawValue;

            // 计算缩放后的索引 (Q16.16): idx = angle * TABLE_SIZE * 2 / PI
            long scaled = (long)raw * SCALE_FACTOR_RAW;
            int idxInt = (int)(scaled >> 32);  // 整数索引

            // 确定象限并映射到 [0, PI/2]
            bool negate;
            if (idxInt < TABLE_SIZE)
            {
                // 第一象限 [0, PI/2)
                negate = false;
            }
            else if (idxInt < TABLE_SIZE * 2)
            {
                // 第二象限 [PI/2, PI) → 映射到 [PI/2, 0)
                idxInt = TABLE_SIZE * 2 - idxInt;
                negate = false;
            }
            else if (idxInt < TABLE_SIZE * 3)
            {
                // 第三象限 [PI, 3PI/2)
                idxInt = idxInt - TABLE_SIZE * 2;
                negate = true;
            }
            else
            {
                // 第四象限 [3PI/2, 2PI)
                idxInt = TABLE_SIZE * 4 - idxInt;
                negate = true;
            }

            // 线性插值
            int frac = (int)((scaled >> 16) & 0xFFFF);
            int v0 = SinTable[idxInt];
            int v1 = SinTable[idxInt < TABLE_SIZE ? idxInt + 1 : TABLE_SIZE];
            long interp = (long)(v1 - v0) * frac >> 16;
            int result = (int)(v0 + interp);

            return new Fix64(negate ? -result : result);
        }

        /// <summary>余弦函数</summary>
        public static Fix64 Cos(Fix64 angle)
        {
            return Sin(angle + PiOver2);
        }

        /// <summary>正切函数。cos(x) ≈ 0 时返回 MaxValue。</summary>
        public static Fix64 Tan(Fix64 angle)
        {
            Fix64 s = Sin(angle);
            Fix64 c = Cos(angle);
            if (c.RawValue == 0) return s.RawValue >= 0 ? Fix64.MaxValue : Fix64.MinValue;
            return s / c;
        }

        #endregion

        #region Asin / Acos / Atan / Atan2

        /// <summary>反正弦函数，返回 [-PI/2, PI/2]</summary>
        public static Fix64 Asin(Fix64 x)
        {
            int absRaw = x.RawValue;
            bool negate = false;
            if (absRaw < 0)
            {
                absRaw = -absRaw;
                negate = true;
            }

            // 钳制到 [0, 1]
            if (absRaw > Fix64.ONE_I) absRaw = Fix64.ONE_I;

            // 查反向索引表
            int idx = AsinIndexTable[absRaw];
            int idxNext = idx < TABLE_SIZE ? idx + 1 : TABLE_SIZE;

            int v0 = SinTable[idx];
            int v1 = SinTable[idxNext];
            int frac = (v1 == v0) ? 0
                : (int)((long)(absRaw - v0) * TABLE_SIZE / (v1 - v0));

            // angle = (idx + frac/TABLE_SIZE) * PI/2 / TABLE_SIZE
            //        = idx * PI/(2*TABLE_SIZE) + frac * PI/(2*TABLE_SIZE^2)
            long angleRaw = (long)idx * PI_OVER_2_RAW / TABLE_SIZE
                          + (long)frac * PI_OVER_2_RAW / (TABLE_SIZE * TABLE_SIZE);

            if (negate) angleRaw = -angleRaw;
            return new Fix64((int)angleRaw);
        }

        /// <summary>反余弦函数，返回 [0, PI]</summary>
        public static Fix64 Acos(Fix64 x)
        {
            return PiOver2 - Asin(x);
        }

        /// <summary>反正切函数，使用恒等式 atan(t) = asin(t / sqrt(1 + t^2))</summary>
        public static Fix64 Atan(Fix64 t)
        {
            if (t.RawValue == 0) return Fix64.Zero;

            Fix64 t2 = t * t;
            Fix64 denom = Fix64.Sqrt(Fix64.One + t2);
            Fix64 ratio = t / denom;
            return Asin(ratio);
        }

        /// <summary>双参数反正切 atan2(y, x)，返回 [-PI, PI]</summary>
        public static Fix64 Atan2(Fix64 y, Fix64 x)
        {
            if (x.RawValue == 0)
            {
                if (y.RawValue > 0) return PiOver2;
                if (y.RawValue < 0) return -PiOver2;
                return Fix64.Zero;
            }

            Fix64 absRatio = Fix64.Abs(y / x);
            Fix64 angle = Atan(absRatio);

            if (x.RawValue > 0)
            {
                return y.RawValue >= 0 ? angle : -angle;
            }
            else
            {
                return y.RawValue >= 0 ? PI - angle : angle - PI;
            }
        }

        #endregion
    }
}
