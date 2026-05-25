using System;
using LockStepLib.Math;

namespace Tests
{
    public static class FixMathTests
    {
        public static void Run()
        {
            Console.WriteLine("--- FixMath Tests ---");

            SinCos();
            Tan();
            AsinAcos();
            AtanAtan2();
            Determinism();
        }

        static void SinCos()
        {
            TestRunner.StartSection("Sin/Cos");

            // sin(0) = 0
            TestRunner.AssertApprox(0.0, FixMath.Sin(Fix64.Zero).ToDouble(), 0.001, "sin(0) = 0");
            // cos(0) = 1
            TestRunner.AssertApprox(1.0, FixMath.Cos(Fix64.Zero).ToDouble(), 0.001, "cos(0) = 1");

            // sin(PI/2) = 1
            TestRunner.AssertApprox(1.0, FixMath.Sin(FixMath.PiOver2).ToDouble(), 0.001, "sin(PI/2) = 1");
            // cos(PI/2) ≈ 0
            TestRunner.AssertApprox(0.0, FixMath.Cos(FixMath.PiOver2).ToDouble(), 0.01, "cos(PI/2) ≈ 0");

            // sin(PI) ≈ 0
            TestRunner.AssertApprox(0.0, FixMath.Sin(FixMath.PI).ToDouble(), 0.01, "sin(PI) ≈ 0");
            // cos(PI) = -1
            TestRunner.AssertApprox(-1.0, FixMath.Cos(FixMath.PI).ToDouble(), 0.01, "cos(PI) = -1");

            // sin(-PI/2) = -1
            TestRunner.AssertApprox(-1.0, FixMath.Sin(-FixMath.PiOver2).ToDouble(), 0.01, "sin(-PI/2) = -1");

            // 大角度归一化: sin(5*PI/2) = sin(PI/2) = 1
            var bigAngle = FixMath.PiOver2 + FixMath.TwoPi + FixMath.TwoPi;
            TestRunner.AssertApprox(1.0, FixMath.Sin(bigAngle).ToDouble(), 0.01, "sin(PI/2 + 4PI) = 1");
        }

        static void Tan()
        {
            TestRunner.StartSection("Tan");

            // tan(0) = 0
            TestRunner.AssertApprox(0.0, FixMath.Tan(Fix64.Zero).ToDouble(), 0.01, "tan(0) = 0");

            // tan(PI/4) ≈ 1
            var piOver4 = FixMath.PI / Fix64.FromInt(4);
            TestRunner.AssertApprox(1.0, FixMath.Tan(piOver4).ToDouble(), 0.01, "tan(PI/4) ≈ 1");

            // tan(-PI/4) ≈ -1
            TestRunner.AssertApprox(-1.0, FixMath.Tan(-piOver4).ToDouble(), 0.01, "tan(-PI/4) ≈ -1");
        }

        static void AsinAcos()
        {
            TestRunner.StartSection("Asin/Acos");

            // asin(0) = 0
            TestRunner.AssertApprox(0.0, FixMath.Asin(Fix64.Zero).ToDouble(), 0.01, "asin(0) = 0");
            // asin(1) = PI/2
            TestRunner.AssertApprox(FixMath.PiOver2.ToDouble(), FixMath.Asin(Fix64.One).ToDouble(), 0.01, "asin(1) = PI/2");

            // acos(1) = 0
            TestRunner.AssertApprox(0.0, FixMath.Acos(Fix64.One).ToDouble(), 0.01, "acos(1) = 0");
            // acos(0) = PI/2
            TestRunner.AssertApprox(FixMath.PiOver2.ToDouble(), FixMath.Acos(Fix64.Zero).ToDouble(), 0.01, "acos(0) = PI/2");
        }

        static void AtanAtan2()
        {
            TestRunner.StartSection("Atan/Atan2");

            // atan(0) = 0
            TestRunner.AssertApprox(0.0, FixMath.Atan(Fix64.Zero).ToDouble(), 0.01, "atan(0) = 0");

            // atan2(y=1, x=0) = PI/2
            var a = FixMath.Atan2(Fix64.One, Fix64.Zero);
            TestRunner.AssertApprox(FixMath.PiOver2.ToDouble(), a.ToDouble(), 0.01, "atan2(1,0) = PI/2");

            // atan2(y=-1, x=0) = -PI/2
            var b = FixMath.Atan2(Fix64.NegativeOne, Fix64.Zero);
            TestRunner.AssertApprox((-FixMath.PiOver2).ToDouble(), b.ToDouble(), 0.01, "atan2(-1,0) = -PI/2");

            // atan2(y=0, x=0) = 0
            var c = FixMath.Atan2(Fix64.Zero, Fix64.Zero);
            TestRunner.AssertApprox(0.0, c.ToDouble(), 0.01, "atan2(0,0) = 0");

            // atan2(y=1, x=1) = PI/4
            var d = FixMath.Atan2(Fix64.One, Fix64.One);
            TestRunner.AssertApprox((FixMath.PI / Fix64.FromInt(4)).ToDouble(), d.ToDouble(), 0.01, "atan2(1,1) = PI/4");

            // atan2(y=-1, x=-1) = -3*PI/4
            var e = FixMath.Atan2(Fix64.NegativeOne, Fix64.NegativeOne);
            TestRunner.AssertApprox(-2.356, e.ToDouble(), 0.01, "atan2(-1,-1) ≈ -3PI/4");
        }

        static void Determinism()
        {
            TestRunner.StartSection("Determinism");

            // 两次相同调用必须返回完全相同的 raw value
            var a1 = FixMath.Sin(Fix64.FromInt(1));
            var a2 = FixMath.Sin(Fix64.FromInt(1));
            TestRunner.AssertEqual(a1.RawValue, a2.RawValue, "sin(1) deterministic");

            var b1 = FixMath.Cos(Fix64.Half);
            var b2 = FixMath.Cos(Fix64.Half);
            TestRunner.AssertEqual(b1.RawValue, b2.RawValue, "cos(0.5) deterministic");

            var c1 = FixMath.Atan2(Fix64.FromInt(3), Fix64.FromInt(4));
            var c2 = FixMath.Atan2(Fix64.FromInt(3), Fix64.FromInt(4));
            TestRunner.AssertEqual(c1.RawValue, c2.RawValue, "atan2(3,4) deterministic");
        }
    }
}
