using System;
using LockStepLib.Math;

namespace Tests
{
    public static class FixMatrixTests
    {
        public static void Run()
        {
            Console.WriteLine("--- FixMatrix Tests ---");

            Identity();
            Rotation();
            Translation();
            Scale();
            TRS();
            Inverse();
            Multiply();
        }

        static void Identity()
        {
            TestRunner.StartSection("Identity");

            var v = new FixVector2(5, 7);
            var result = FixMatrix.Identity.TransformPoint(v);
            TestRunner.AssertApprox(5.0, result.X.ToDouble(), 0.01, "identity.x=5");
            TestRunner.AssertApprox(7.0, result.Y.ToDouble(), 0.01, "identity.y=7");
        }

        static void Rotation()
        {
            TestRunner.StartSection("Rotation");

            var rot90 = FixMatrix.CreateRotation(FixMath.PiOver2);
            var v = FixVector2.Right;
            var result = rot90.TransformPoint(v);
            TestRunner.AssertApprox(0.0, result.X.ToDouble(), 0.05, "rot90(1,0).x≈0");
            TestRunner.AssertApprox(1.0, result.Y.ToDouble(), 0.05, "rot90(1,0).y≈1");

            // rotation should not affect direction vector magnitude
            var dir = new FixVector2(3, 4);
            var rotatedDir = rot90.TransformDirection(dir);
            TestRunner.AssertApprox(5.0, rotatedDir.Length().ToDouble(), 0.1, "rot preserves length");
        }

        static void Translation()
        {
            TestRunner.StartSection("Translation");

            var trans = FixMatrix.CreateTranslation(Fix64.FromInt(10), Fix64.FromInt(20));
            var v = new FixVector2(1, 2);
            var result = trans.TransformPoint(v);
            TestRunner.AssertEqual(11, result.X.ToInt(), "trans(10,20)(1,2).x=11");
            TestRunner.AssertEqual(22, result.Y.ToInt(), "trans(10,20)(1,2).y=22");

            // direction unaffected by translation
            var dir = trans.TransformDirection(v);
            TestRunner.AssertEqual(1, dir.X.ToInt(), "trans direction.x=1");
        }

        static void Scale()
        {
            TestRunner.StartSection("Scale");

            var scale = FixMatrix.CreateScale(Fix64.FromInt(2), Fix64.FromInt(3));
            var v = new FixVector2(3, 4);
            var result = scale.TransformPoint(v);
            TestRunner.AssertEqual(6, result.X.ToInt(), "scale(2,3)(3,4).x=6");
            TestRunner.AssertEqual(12, result.Y.ToInt(), "scale(2,3)(3,4).y=12");
        }

        static void TRS()
        {
            TestRunner.StartSection("TRS");

            // 平移(5,0) + 旋转90° + 缩放(2,2): 先缩放→旋转→平移
            var trs = FixMatrix.CreateTRS(
                new FixVector2(5, 0),
                FixMath.PiOver2,
                new FixVector2(2, 2)
            );

            var v = new FixVector2(1, 0);
            var result = trs.TransformPoint(v);
            // scale: (2,0) → rotate: (0,2) → translate: (5,2)
            TestRunner.AssertApprox(5.0, result.X.ToDouble(), 0.1, "TRS(1,0).x≈5");
            TestRunner.AssertApprox(2.0, result.Y.ToDouble(), 0.1, "TRS(1,0).y≈2");
        }

        static void Inverse()
        {
            TestRunner.StartSection("Inverse");

            var trans = FixMatrix.CreateTranslation(Fix64.FromInt(10), Fix64.Zero);
            var inv = trans.Inverse();
            var v = new FixVector2(15, 0);
            var result = inv.TransformPoint(v);
            TestRunner.AssertApprox(5.0, result.X.ToDouble(), 0.01, "inv(translate10)(15,0).x=5");

            // inverse of identity = identity
            var idInv = FixMatrix.Identity.Inverse();
            TestRunner.AssertApprox(1.0, idInv.M11.ToDouble(), 0.01, "inv(I).M11=1");
        }

        static void Multiply()
        {
            TestRunner.StartSection("Multiply");

            var rot = FixMatrix.CreateRotation(FixMath.PiOver2);
            var trans = FixMatrix.CreateTranslation(Fix64.FromInt(10), Fix64.Zero);
            var combined = rot * trans; // rotate, then translate

            var v = new FixVector2(1, 0);
            var result = combined.TransformPoint(v);
            // translate(10,0) then rotate(90): (1+10, 0) → (0, 11)
            TestRunner.AssertApprox(0.0, result.X.ToDouble(), 0.1, "rot*trans(1,0).x≈0");
            TestRunner.AssertApprox(11.0, result.Y.ToDouble(), 0.1, "rot*trans(1,0).y≈11");
        }
    }
}
