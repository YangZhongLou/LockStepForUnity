using System;
using LockStepLib.Math;

namespace Tests
{
    public static class FixVector3Tests
    {
        public static void Run()
        {
            Console.WriteLine("--- FixVector3 Tests ---");

            BasicOps();
            DotCross();
            LengthNormalize();
            Project();
        }

        static void BasicOps()
        {
            TestRunner.StartSection("Basic Ops");

            var a = new FixVector3(1, 2, 3);
            var b = new FixVector3(4, 5, 6);

            var sum = a + b;
            TestRunner.AssertEqual(5, sum.X.ToInt(), "1+4=5");
            TestRunner.AssertEqual(7, sum.Y.ToInt(), "2+5=7");
            TestRunner.AssertEqual(9, sum.Z.ToInt(), "3+6=9");

            var scaled = a * Fix64.FromInt(2);
            TestRunner.AssertEqual(2, scaled.X.ToInt(), "1*2=2");
            TestRunner.AssertEqual(6, scaled.Z.ToInt(), "3*2=6");
        }

        static void DotCross()
        {
            TestRunner.StartSection("Dot/Cross");

            var x = FixVector3.Right;
            var y = FixVector3.Up;
            var z = FixVector3.Forward;

            TestRunner.AssertApprox(0.0, FixVector3.Dot(x, y).ToDouble(), 0.01, "dot(x,y)=0");
            TestRunner.AssertApprox(1.0, FixVector3.Dot(x, x).ToDouble(), 0.01, "dot(x,x)=1");

            var cross = FixVector3.Cross(x, y);
            TestRunner.AssertApprox(0.0, cross.X.ToDouble(), 0.01, "cross(x,y).x=0");
            TestRunner.AssertApprox(0.0, cross.Y.ToDouble(), 0.01, "cross(x,y).y=0");
            TestRunner.AssertApprox(1.0, cross.Z.ToDouble(), 0.01, "cross(x,y).z=1");
        }

        static void LengthNormalize()
        {
            TestRunner.StartSection("Length/Normalize");

            var v = new FixVector3(2, 3, 6);
            var lenSqr = 2 * 2 + 3 * 3 + 6 * 6;
            TestRunner.AssertApprox(lenSqr, v.LengthSqr().ToDouble(), 0.01, "|(2,3,6)|^2=49");

            var n = v.Normalized();
            TestRunner.AssertApprox(1.0, n.Length().ToDouble(), 0.01, "|norm|=1");

            TestRunner.AssertApprox(7.0, FixVector3.Distance(FixVector3.Zero, v).ToDouble(), 0.1, "dist=7");
        }

        static void Project()
        {
            TestRunner.StartSection("Project");

            var v = new FixVector3(3, 4, 0);
            var onto = FixVector3.Right;
            var proj = FixVector3.Project(v, onto);

            TestRunner.AssertApprox(3.0, proj.X.ToDouble(), 0.01, "proj(3,4,0)→x.x=3");
            TestRunner.AssertApprox(0.0, proj.Y.ToDouble(), 0.01, "proj(3,4,0)→x.y=0");
        }
    }
}
