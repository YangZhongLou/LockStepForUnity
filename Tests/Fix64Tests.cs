using System;
using LockStepLib.Math;

namespace Tests
{
    public static class Fix64Tests
    {
        public static void Run()
        {
            Console.WriteLine("--- Fix64 Tests ---");

            Arithmetic();
            Comparison();
            FloorCeilRound();
            Sqrt();
            LerpClamp();
            Constants();
        }

        static void Arithmetic()
        {
            TestRunner.StartSection("Arithmetic");

            var a = Fix64.FromInt(3);
            var b = Fix64.FromInt(7);

            TestRunner.AssertEqual(10.0, (a + b).ToDouble(), 0.001, "3 + 7 = 10");
            TestRunner.AssertEqual(-4.0, (a - b).ToDouble(), 0.001, "3 - 7 = -4");
            TestRunner.AssertEqual(21.0, (a * b).ToDouble(), 0.001, "3 * 7 = 21");
            TestRunner.AssertApprox(0.4286, (a / b).ToDouble(), 0.001, "3 / 7 ≈ 0.4286");

            TestRunner.AssertEqual(0.0, (Fix64.Zero * Fix64.FromInt(5)).ToDouble(), 0.001, "0 * 5 = 0");
            TestRunner.AssertEqual(-3.0, (-a).ToDouble(), 0.001, "-3 = -3");

            var c = Fix64.FromDouble(2.5);
            TestRunner.AssertApprox(2.5, c.ToDouble(), 0.001, "FromDouble(2.5)");

            // 除法边界: 除以自身 = 1
            var one = a / a;
            TestRunner.AssertEqual(1.0, one.ToDouble(), 0.001, "3 / 3 = 1");
        }

        static void Comparison()
        {
            TestRunner.StartSection("Comparison");

            var a = Fix64.FromInt(3);
            var b = Fix64.FromInt(7);
            var c = Fix64.FromInt(3);

            TestRunner.AssertEqual(true, a == c, "3 == 3");
            TestRunner.AssertEqual(true, a != b, "3 != 7");
            TestRunner.AssertEqual(true, a < b, "3 < 7");
            TestRunner.AssertEqual(true, b > a, "7 > 3");
            TestRunner.AssertEqual(true, a <= c, "3 <= 3");
            TestRunner.AssertEqual(true, b >= a, "7 >= 3");

            var neg = Fix64.FromInt(-1);
            TestRunner.AssertEqual(true, neg < Fix64.Zero, "-1 < 0");
        }

        static void FloorCeilRound()
        {
            TestRunner.StartSection("Floor/Ceil/Round");

            var v = Fix64.FromDouble(3.7);
            TestRunner.AssertEqual(3.0, Fix64.Floor(v).ToDouble(), 0.001, "floor(3.7) = 3");
            TestRunner.AssertEqual(4.0, Fix64.Ceil(v).ToDouble(), 0.001, "ceil(3.7) = 4");
            TestRunner.AssertEqual(4.0, Fix64.Round(v).ToDouble(), 0.001, "round(3.7) = 4");

            var n = Fix64.FromDouble(-2.3);
            TestRunner.AssertEqual(-3.0, Fix64.Floor(n).ToDouble(), 0.001, "floor(-2.3) = -3");
            TestRunner.AssertEqual(-2.0, Fix64.Ceil(n).ToDouble(), 0.001, "ceil(-2.3) = -2");

            TestRunner.AssertEqual(4, Fix64.Round(Fix64.FromDouble(4.25)).ToInt(), "round(4.25) = 4");
            TestRunner.AssertEqual(5, Fix64.Round(Fix64.FromDouble(4.75)).ToInt(), "round(4.75) = 5");
        }

        static void Sqrt()
        {
            TestRunner.StartSection("Sqrt");

            TestRunner.AssertEqual(2.0, Fix64.Sqrt(Fix64.FromInt(4)).ToDouble(), 0.001, "sqrt(4) = 2");
            TestRunner.AssertEqual(3.0, Fix64.Sqrt(Fix64.FromInt(9)).ToDouble(), 0.001, "sqrt(9) = 3");
            TestRunner.AssertEqual(0.0, Fix64.Sqrt(Fix64.Zero).ToDouble(), 0.001, "sqrt(0) = 0");
            TestRunner.AssertEqual(0.0, Fix64.Sqrt(Fix64.FromInt(-1)).ToDouble(), 0.001, "sqrt(-1) = 0");

            // sqrt(2) ≈ 1.414
            TestRunner.AssertApprox(1.414, Fix64.Sqrt(Fix64.FromInt(2)).ToDouble(), 0.001, "sqrt(2) ≈ 1.414");

            // sqrt(0.25) = 0.5
            var q = Fix64.FromDouble(0.25);
            TestRunner.AssertApprox(0.5, Fix64.Sqrt(q).ToDouble(), 0.01, "sqrt(0.25) = 0.5");
        }

        static void LerpClamp()
        {
            TestRunner.StartSection("Lerp/Clamp");

            var a = Fix64.FromInt(0);
            var b = Fix64.FromInt(10);
            var t = Fix64.Half;
            TestRunner.AssertEqual(5.0, Fix64.Lerp(a, b, t).ToDouble(), 0.001, "lerp(0,10,0.5) = 5");

            // t beyond [0,1] should clamp
            TestRunner.AssertEqual(10.0, Fix64.Lerp(a, b, Fix64.FromInt(2)).ToDouble(), 0.001, "lerp(0,10,2) = 10");

            var v = Fix64.FromInt(5);
            TestRunner.AssertEqual(5.0, Fix64.Clamp(v, Fix64.Zero, Fix64.FromInt(10)).ToDouble(), 0.001, "clamp(5,0,10) = 5");
            TestRunner.AssertEqual(0.0, Fix64.Clamp(Fix64.FromInt(-1), Fix64.Zero, Fix64.FromInt(10)).ToDouble(), 0.001, "clamp(-1,0,10) = 0");
            TestRunner.AssertEqual(10.0, Fix64.Clamp(Fix64.FromInt(100), Fix64.Zero, Fix64.FromInt(10)).ToDouble(), 0.001, "clamp(100,0,10) = 10");
        }

        static void Constants()
        {
            TestRunner.StartSection("Constants");

            TestRunner.AssertEqual(true, Fix64.Pi > Fix64.FromInt(3), "Pi > 3");
            TestRunner.AssertEqual(true, Fix64.Pi < Fix64.FromDouble(3.2), "Pi < 3.2");
            TestRunner.AssertEqual(true, Fix64.E > Fix64.FromDouble(2.7), "E > 2.7");
            TestRunner.AssertEqual(true, Fix64.E < Fix64.FromDouble(2.8), "E < 2.8");

            TestRunner.AssertEqual(1.0, Fix64.One.ToDouble(), 0.001, "One = 1");
            TestRunner.AssertEqual(0.0, Fix64.Zero.ToDouble(), 0.001, "Zero = 0");
        }
    }
}
