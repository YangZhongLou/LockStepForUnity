using System;

namespace LockStepLib.Math
{
    /// <summary>
    /// 三维定点向量。所有运算确定性。
    /// </summary>
    public readonly struct FixVector3 : IEquatable<FixVector3>
    {
        public readonly Fix64 X;
        public readonly Fix64 Y;
        public readonly Fix64 Z;

        #region 常量

        public static readonly FixVector3 Zero = new FixVector3(Fix64.Zero, Fix64.Zero, Fix64.Zero);
        public static readonly FixVector3 One = new FixVector3(Fix64.One, Fix64.One, Fix64.One);
        public static readonly FixVector3 Up = new FixVector3(Fix64.Zero, Fix64.One, Fix64.Zero);
        public static readonly FixVector3 Down = new FixVector3(Fix64.Zero, Fix64.NegativeOne, Fix64.Zero);
        public static readonly FixVector3 Left = new FixVector3(Fix64.NegativeOne, Fix64.Zero, Fix64.Zero);
        public static readonly FixVector3 Right = new FixVector3(Fix64.One, Fix64.Zero, Fix64.Zero);
        public static readonly FixVector3 Forward = new FixVector3(Fix64.Zero, Fix64.Zero, Fix64.One);
        public static readonly FixVector3 Back = new FixVector3(Fix64.Zero, Fix64.Zero, Fix64.NegativeOne);

        #endregion

        #region 构造

        public FixVector3(Fix64 x, Fix64 y, Fix64 z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public FixVector3(int x, int y, int z)
        {
            X = Fix64.FromInt(x);
            Y = Fix64.FromInt(y);
            Z = Fix64.FromInt(z);
        }

        #endregion

        #region 运算符

        public static FixVector3 operator +(FixVector3 a, FixVector3 b) => new FixVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static FixVector3 operator -(FixVector3 a, FixVector3 b) => new FixVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static FixVector3 operator -(FixVector3 a) => new FixVector3(-a.X, -a.Y, -a.Z);
        public static FixVector3 operator *(FixVector3 a, Fix64 scalar) => new FixVector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        public static FixVector3 operator *(Fix64 scalar, FixVector3 a) => new FixVector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        public static FixVector3 operator /(FixVector3 a, Fix64 scalar) => new FixVector3(a.X / scalar, a.Y / scalar, a.Z / scalar);
        public static bool operator ==(FixVector3 a, FixVector3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        public static bool operator !=(FixVector3 a, FixVector3 b) => !(a == b);

        #endregion

        #region 向量运算

        /// <summary>点积</summary>
        public static Fix64 Dot(FixVector3 a, FixVector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        /// <summary>3D 叉积</summary>
        public static FixVector3 Cross(FixVector3 a, FixVector3 b)
        {
            return new FixVector3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }

        public Fix64 LengthSqr() => X * X + Y * Y + Z * Z;

        public Fix64 Length() => Fix64.Sqrt(LengthSqr());

        public static Fix64 DistanceSqr(FixVector3 a, FixVector3 b) => (b - a).LengthSqr();

        public static Fix64 Distance(FixVector3 a, FixVector3 b) => (b - a).Length();

        /// <summary>归一化。零向量返回 (1, 0, 0)。</summary>
        public FixVector3 Normalized()
        {
            Fix64 len = Length();
            if (len.RawValue == 0) return Right;
            return this / len;
        }

        public static FixVector3 Lerp(FixVector3 a, FixVector3 b, Fix64 t)
        {
            return a + (b - a) * t;
        }

        public static FixVector3 MoveTowards(FixVector3 current, FixVector3 target, Fix64 maxDistance)
        {
            FixVector3 delta = target - current;
            Fix64 dist = delta.Length();
            if (dist.RawValue <= maxDistance.RawValue || dist.RawValue == 0)
                return target;
            return current + delta / dist * maxDistance;
        }

        public static Fix64 Angle(FixVector3 from, FixVector3 to)
        {
            Fix64 dot = Dot(from.Normalized(), to.Normalized());
            return FixMath.Acos(Fix64.Clamp(dot, Fix64.NegativeOne, Fix64.One));
        }

        public static FixVector3 Reflect(FixVector3 direction, FixVector3 normal)
        {
            Fix64 d = Fix64.FromInt(2) * Dot(direction, normal);
            return direction - d * normal;
        }

        /// <summary>投影到另一个向量上</summary>
        public static FixVector3 Project(FixVector3 vector, FixVector3 onNormal)
        {
            Fix64 lenSqr = onNormal.LengthSqr();
            if (lenSqr.RawValue == 0) return Zero;
            return onNormal * (Dot(vector, onNormal) / lenSqr);
        }

        /// <summary>按分量取最小值</summary>
        public static FixVector3 Min(FixVector3 a, FixVector3 b)
            => new FixVector3(Fix64.Min(a.X, b.X), Fix64.Min(a.Y, b.Y), Fix64.Min(a.Z, b.Z));

        /// <summary>按分量取最大值</summary>
        public static FixVector3 Max(FixVector3 a, FixVector3 b)
            => new FixVector3(Fix64.Max(a.X, b.X), Fix64.Max(a.Y, b.Y), Fix64.Max(a.Z, b.Z));

        #endregion

        #region Equals / Object

        public bool Equals(FixVector3 other) => X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object obj) => obj is FixVector3 v && Equals(v);

        public override int GetHashCode() => X.RawValue ^ (Y.RawValue << 9) ^ (Z.RawValue << 18);

        public override string ToString() => $"({X.ToDouble():F4}, {Y.ToDouble():F4}, {Z.ToDouble():F4})";

        #endregion
    }
}
