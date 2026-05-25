using System;

namespace LockStepLib.Math
{
    /// <summary>
    /// 3x3 仿射变换矩阵 (行主序)，用于 2D 空间变换。
    /// 支持旋转、平移、缩放及其组合。
    /// 布局:
    /// [M11 M12 M13]   [x]
    /// [M21 M22 M23] * [y]
    /// [M31 M32 M33]   [1]
    /// </summary>
    public readonly struct FixMatrix : IEquatable<FixMatrix>
    {
        public readonly Fix64 M11, M12, M13;
        public readonly Fix64 M21, M22, M23;
        public readonly Fix64 M31, M32, M33;

        #region 常量

        public static readonly FixMatrix Identity = new FixMatrix(
            Fix64.One,  Fix64.Zero, Fix64.Zero,
            Fix64.Zero, Fix64.One,  Fix64.Zero,
            Fix64.Zero, Fix64.Zero, Fix64.One
        );

        #endregion

        #region 构造

        public FixMatrix(
            Fix64 m11, Fix64 m12, Fix64 m13,
            Fix64 m21, Fix64 m22, Fix64 m23,
            Fix64 m31, Fix64 m32, Fix64 m33)
        {
            M11 = m11; M12 = m12; M13 = m13;
            M21 = m21; M22 = m22; M23 = m23;
            M31 = m31; M32 = m32; M33 = m33;
        }

        #endregion

        #region 工厂方法

        /// <summary>2D 旋转矩阵 (弧度，逆时针)</summary>
        public static FixMatrix CreateRotation(Fix64 radians)
        {
            Fix64 c = FixMath.Cos(radians);
            Fix64 s = FixMath.Sin(radians);
            return new FixMatrix(
                c,   -s,   Fix64.Zero,
                s,    c,   Fix64.Zero,
                Fix64.Zero, Fix64.Zero, Fix64.One
            );
        }

        /// <summary>2D 平移矩阵</summary>
        public static FixMatrix CreateTranslation(Fix64 x, Fix64 y)
        {
            return new FixMatrix(
                Fix64.One,  Fix64.Zero, x,
                Fix64.Zero, Fix64.One,  y,
                Fix64.Zero, Fix64.Zero, Fix64.One
            );
        }

        /// <summary>2D 平移矩阵 (向量)</summary>
        public static FixMatrix CreateTranslation(FixVector2 offset)
        {
            return CreateTranslation(offset.X, offset.Y);
        }

        /// <summary>2D 缩放矩阵</summary>
        public static FixMatrix CreateScale(Fix64 x, Fix64 y)
        {
            return new FixMatrix(
                x,         Fix64.Zero, Fix64.Zero,
                Fix64.Zero, y,         Fix64.Zero,
                Fix64.Zero, Fix64.Zero, Fix64.One
            );
        }

        /// <summary>2D 均匀缩放</summary>
        public static FixMatrix CreateScale(Fix64 uniformScale)
        {
            return CreateScale(uniformScale, uniformScale);
        }

        /// <summary>TRS 组合: 先缩放、再旋转、最后平移</summary>
        public static FixMatrix CreateTRS(FixVector2 translation, Fix64 rotation, FixVector2 scale)
        {
            return CreateTranslation(translation)
                 * CreateRotation(rotation)
                 * CreateScale(scale.X, scale.Y);
        }

        #endregion

        #region 运算符

        public static FixMatrix operator *(FixMatrix a, FixMatrix b)
        {
            return new FixMatrix(
                a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
                a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
                a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,

                a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
                a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
                a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,

                a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
                a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
                a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33
            );
        }

        public static bool operator ==(FixMatrix a, FixMatrix b) => a.Equals(b);
        public static bool operator !=(FixMatrix a, FixMatrix b) => !a.Equals(b);

        #endregion

        #region 变换

        /// <summary>变换 2D 点 (应用平移)</summary>
        public FixVector2 TransformPoint(FixVector2 point)
        {
            return new FixVector2(
                M11 * point.X + M12 * point.Y + M13,
                M21 * point.X + M22 * point.Y + M23
            );
        }

        /// <summary>变换 2D 方向向量 (不应用平移)</summary>
        public FixVector2 TransformDirection(FixVector2 direction)
        {
            return new FixVector2(
                M11 * direction.X + M12 * direction.Y,
                M21 * direction.X + M22 * direction.Y
            );
        }

        /// <summary>变换 3D 向量 (完整 3x3 乘法)</summary>
        public FixVector3 TransformVector(FixVector3 v)
        {
            return new FixVector3(
                M11 * v.X + M12 * v.Y + M13 * v.Z,
                M21 * v.X + M22 * v.Y + M23 * v.Z,
                M31 * v.X + M32 * v.Y + M33 * v.Z
            );
        }

        #endregion

        #region 矩阵运算

        /// <summary>行列式</summary>
        public Fix64 Determinant()
        {
            return M11 * (M22 * M33 - M23 * M32)
                 - M12 * (M21 * M33 - M23 * M31)
                 + M13 * (M21 * M32 - M22 * M31);
        }

        /// <summary>转置</summary>
        public FixMatrix Transpose()
        {
            return new FixMatrix(
                M11, M21, M31,
                M12, M22, M32,
                M13, M23, M33
            );
        }

        /// <summary>逆矩阵。行列式为 0 时返回单位矩阵。</summary>
        public FixMatrix Inverse()
        {
            Fix64 det = Determinant();
            if (det.RawValue == 0) return Identity;

            // 伴随矩阵 / 行列式
            Fix64 invDet = Fix64.One / det;
            return new FixMatrix(
                (M22 * M33 - M23 * M32) * invDet,
                (M13 * M32 - M12 * M33) * invDet,
                (M12 * M23 - M13 * M22) * invDet,

                (M23 * M31 - M21 * M33) * invDet,
                (M11 * M33 - M13 * M31) * invDet,
                (M13 * M21 - M11 * M23) * invDet,

                (M21 * M32 - M22 * M31) * invDet,
                (M12 * M31 - M11 * M32) * invDet,
                (M11 * M22 - M12 * M21) * invDet
            );
        }

        #endregion

        #region Equals / Object

        public bool Equals(FixMatrix other)
        {
            return M11 == other.M11 && M12 == other.M12 && M13 == other.M13
                && M21 == other.M21 && M22 == other.M22 && M23 == other.M23
                && M31 == other.M31 && M32 == other.M32 && M33 == other.M33;
        }

        public override bool Equals(object obj) => obj is FixMatrix m && Equals(m);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = M11.RawValue;
                h = h * 31 + M12.RawValue;
                h = h * 31 + M13.RawValue;
                h = h * 31 + M21.RawValue;
                h = h * 31 + M22.RawValue;
                h = h * 31 + M23.RawValue;
                h = h * 31 + M31.RawValue;
                h = h * 31 + M32.RawValue;
                h = h * 31 + M33.RawValue;
                return h;
            }
        }

        public override string ToString()
        {
            return $"[{M11.ToDouble():F3}, {M12.ToDouble():F3}, {M13.ToDouble():F3}]\n"
                 + $"[{M21.ToDouble():F3}, {M22.ToDouble():F3}, {M23.ToDouble():F3}]\n"
                 + $"[{M31.ToDouble():F3}, {M32.ToDouble():F3}, {M33.ToDouble():F3}]";
        }

        #endregion
    }
}
