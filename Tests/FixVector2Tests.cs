using System;
using LockStepLib.Math;

namespace Tests
{
    public static class FixVector2Tests
    {
        public static void Run()
        {
            Console.WriteLine("--- FixVector2 Tests ---");

            BasicOps();
            DotCross();
            LengthNormalize();
            LerpMoveTowards();
            Angle();
            Reflect();
        }

        static void BasicOps()
        {
            TestRunner.StartSection("Basic Ops");

            var a = new FixVector2(3, 4);
            var b = new FixVector2(1, 2);

            var sum = a + b;
            TestRunner.AssertEqual(4, sum.X.ToInt(), "3+1=4");
            TestRunner.AssertEqual(6, sum.Y.ToInt(), "4+2=6");

            var diff = a - b;
            TestRunner.AssertEqual(2, diff.X.ToInt(), "3-1=2");
            TestRunner.AssertEqual(2, diff.Y.ToInt(), "4-2=2");

            var scaled = a * Fix64.FromInt(2);
            TestRunner.AssertEqual(6, scaled.X.ToInt(), "3*2=6");
            TestRunner.AssertEqual(8, scaled.Y.ToInt(), "4*2=8");

            var halved = a / Fix64.FromInt(2);
            TestRunner.AssertApprox(1.5, halved.X.ToDouble(), 0.01, "3/2≈1.5");
            TestRunner.AssertApprox(2.0, halved.Y.ToDouble(), 0.01, "4/2=2");
        }

        static void DotCross()
        {
            TestRunner.StartSection("Dot/Cross");

            var a = new FixVector2(1, 0);
            var b = new FixVector2(0, 1);
            var c = new FixVector2(-1, 0);

            TestRunner.AssertApprox(0.0, FixVector2.Dot(a, b).ToDouble(), 0.01, "dot((1,0),(0,1))=0");
            TestRunner.AssertApprox(1.0, FixVector2.Dot(a, a).ToDouble(), 0.01, "dot((1,0),(1,0))=1");
            TestRunner.AssertApprox(-1.0, FixVector2.Dot(a, c).ToDouble(), 0.01, "dot((1,0),(-1,0))=-1");

            // 2D cross returns scalar
            TestRunner.AssertApprox(1.0, FixVector2.Cross(a, b).ToDouble(), 0.01, "cross((1,0),(0,1))=1");
            TestRunner.AssertApprox(-1.0, FixVector2.Cross(b, a).ToDouble(), 0.01, "cross((0,1),(1,0))=-1");
        }

        static void LengthNormalize()
        {
            TestRunner.StartSection("Length/Normalize");

            var v = new FixVector2(3, 4);
            TestRunner.AssertApprox(5.0, v.Length().ToDouble(), 0.01, "|(3,4)|=5");
            TestRunner.AssertApprox(25.0, v.LengthSqr().ToDouble(), 0.01, "|(3,4)|^2=25");

            var n = v.Normalized();
            TestRunner.AssertApprox(0.6, n.X.ToDouble(), 0.01, "norm(3,4).x≈0.6");
            TestRunner.AssertApprox(0.8, n.Y.ToDouble(), 0.01, "norm(3,4).y≈0.8");
            TestRunner.AssertApprox(1.0, n.Length().ToDouble(), 0.01, "|norm|=1");

            // zero vector normalize returns (1,0)
            var zero = FixVector2.Zero.Normalized();
            TestRunner.AssertEqual(1, zero.X.ToInt(), "norm(0,0).x=1");
            TestRunner.AssertEqual(0, zero.Y.ToInt(), "norm(0,0).y=0");

            // distance
            TestRunner.AssertApprox(5.0, FixVector2.Distance(new FixVector2(0, 0), new FixVector2(3, 4)).ToDouble(), 0.01, "dist=5");
        }

        static void LerpMoveTowards()
        {
            TestRunner.StartSection("Lerp/MoveTowards");

            var a = FixVector2.Zero;
            var b = new FixVector2(10, 0);
            var mid = FixVector2.Lerp(a, b, Fix64.Half);
            TestRunner.AssertApprox(5.0, mid.X.ToDouble(), 0.01, "lerp midpoint x=5");

            var moved = FixVector2.MoveTowards(a, b, Fix64.FromInt(3));
            TestRunner.AssertApprox(3.0, moved.X.ToDouble(), 0.01, "move 3 towards 10");
        }

        static void Angle()
        {
            TestRunner.StartSection("Angle");

            var right = FixVector2.Right;
            var up = FixVector2.Up;

            var angle = FixVector2.Angle(right, up);
            TestRunner.AssertApprox(System.Math.PI / 2, angle.ToDouble(), 0.01, "angle(right,up)≈90°");

            var signed = FixVector2.SignedAngle(right, up);
            TestRunner.AssertApprox(System.Math.PI / 2, signed.ToDouble(), 0.01, "signedAngle(right,up)≈+90°");

            var signedNeg = FixVector2.SignedAngle(up, right);
            TestRunner.AssertApprox(-System.Math.PI / 2, signedNeg.ToDouble(), 0.01, "signedAngle(up,right)≈-90°");
        }

        static void Reflect()
        {
            TestRunner.StartSection("Reflect");

            var dir = new FixVector2(1, -1);
            var normal = FixVector2.Up;
            var reflected = FixVector2.Reflect(dir, normal);

            TestRunner.AssertApprox(1.0, reflected.X.ToDouble(), 0.01, "reflect(1,-1)up.x=1");
            TestRunner.AssertApprox(1.0, reflected.Y.ToDouble(), 0.01, "reflect(1,-1)up.y=1");
        }
    }
}
