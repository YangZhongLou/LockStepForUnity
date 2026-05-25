using System;

namespace LockStepLib.Math
{
    /// <summary>
    /// 二维定点向量。所有运算确定性。
    /// </summary>
    public readonly struct FixVector2 : IEquatable<FixVector2>
    {
        public readonly Fix64 X;
        public readonly Fix64 Y;

        #region 常量

        public static readonly FixVector2 Zero = new FixVector2(Fix64.Zero, Fix64.Zero);
        public static readonly FixVector2 One = new FixVector2(Fix64.One, Fix64.One);
        public static readonly FixVector2 Up = new FixVector2(Fix64.Zero, Fix64.One);
        public static readonly FixVector2 Down = new FixVector2(Fix64.Zero, Fix64.NegativeOne);
        public static readonly FixVector2 Left = new FixVector2(Fix64.NegativeOne, Fix64.Zero);
        public static readonly FixVector2 Right = new FixVector2(Fix64.One, Fix64.Zero);

        #endregion

        #region 构造

        public FixVector2(Fix64 x, Fix64 y)
        {
            X = x;
            Y = y;
        }

        public FixVector2(int x, int y)
        {
            X = Fix64.FromInt(x);
            Y = Fix64.FromInt(y);
        }

        #endregion

        #region 运算符

        public static FixVector2 operator +(FixVector2 a, FixVector2 b) => new FixVector2(a.X + b.X, a.Y + b.Y);
        public static FixVector2 operator -(FixVector2 a, FixVector2 b) => new FixVector2(a.X - b.X, a.Y - b.Y);
        public static FixVector2 operator -(FixVector2 a) => new FixVector2(-a.X, -a.Y);
        public static FixVector2 operator *(FixVector2 a, Fix64 scalar) => new FixVector2(a.X * scalar, a.Y * scalar);
        public static FixVector2 operator *(Fix64 scalar, FixVector2 a) => new FixVector2(a.X * scalar, a.Y * scalar);
        public static FixVector2 operator /(FixVector2 a, Fix64 scalar) => new FixVector2(a.X / scalar, a.Y / scalar);
        public static bool operator ==(FixVector2 a, FixVector2 b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(FixVector2 a, FixVector2 b) => !(a == b);

        #endregion

        #region 向量运算

        /// <summary>点积</summary>
        public static Fix64 Dot(FixVector2 a, FixVector2 b) => a.X * b.X + a.Y * b.Y;

        /// <summary>2D 叉积 (标量): a × b = |a||b|sin(θ)</summary>
        public static Fix64 Cross(FixVector2 a, FixVector2 b) => a.X * b.Y - a.Y * b.X;

        public Fix64 LengthSqr() => X * X + Y * Y;

        public Fix64 Length() => Fix64.Sqrt(LengthSqr());

        /// <summary>距离平方</summary>
        public static Fix64 DistanceSqr(FixVector2 a, FixVector2 b) => (b - a).LengthSqr();

        /// <summary>欧几里得距离</summary>
        public static Fix64 Distance(FixVector2 a, FixVector2 b) => (b - a).Length();

        /// <summary>归一化向量。零向量返回 (1, 0)。</summary>
        public FixVector2 Normalized()
        {
            Fix64 len = Length();
            if (len.RawValue == 0) return Right;
            return this / len;
        }

        /// <summary>线性插值</summary>
        public static FixVector2 Lerp(FixVector2 a, FixVector2 b, Fix64 t)
        {
            return a + (b - a) * t;
        }

        /// <summary>向目标方向移动，不超过 maxDistance</summary>
        public static FixVector2 MoveTowards(FixVector2 current, FixVector2 target, Fix64 maxDistance)
        {
            FixVector2 delta = target - current;
            Fix64 dist = delta.Length();
            if (dist.RawValue <= maxDistance.RawValue || dist.RawValue == 0)
                return target;
            return current + delta / dist * maxDistance;
        }

        /// <summary>从角度创建单位向量 (角度为弧度，从 X 轴正方向逆时针)</summary>
        public static FixVector2 FromAngle(Fix64 radians)
        {
            return new FixVector2(FixMath.Cos(radians), FixMath.Sin(radians));
        }

        /// <summary>两个向量的夹角 (弧度)</summary>
        public static Fix64 Angle(FixVector2 from, FixVector2 to)
        {
            Fix64 dot = Dot(from.Normalized(), to.Normalized());
            return FixMath.Acos(Fix64.Clamp(dot, Fix64.NegativeOne, Fix64.One));
        }

        /// <summary>符号角度 (带方向，-PI 到 PI)</summary>
        public static Fix64 SignedAngle(FixVector2 from, FixVector2 to)
        {
            Fix64 angle = Angle(from, to);
            Fix64 sign = Cross(from, to).RawValue >= 0 ? Fix64.One : Fix64.NegativeOne;
            return angle * sign;
        }

        /// <summary>反射向量 (关于法线反射)</summary>
        public static FixVector2 Reflect(FixVector2 direction, FixVector2 normal)
        {
            Fix64 d = Fix64.FromInt(2) * Dot(direction, normal);
            return direction - d * normal;
        }

        /// <summary>按分量取最小值</summary>
        public static FixVector2 Min(FixVector2 a, FixVector2 b) => new FixVector2(Fix64.Min(a.X, b.X), Fix64.Min(a.Y, b.Y));

        /// <summary>按分量取最大值</summary>
        public static FixVector2 Max(FixVector2 a, FixVector2 b) => new FixVector2(Fix64.Max(a.X, b.X), Fix64.Max(a.Y, b.Y));

        #endregion

        #region Equals / Object

        public bool Equals(FixVector2 other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is FixVector2 v && Equals(v);

        public override int GetHashCode() => X.RawValue ^ (Y.RawValue << 13 | (int)((uint)Y.RawValue >> 19));

        public override string ToString() => $"({X.ToDouble():F4}, {Y.ToDouble():F4})";

        #endregion
    }
}
