using System;

namespace Tests
{
    /// <summary>
    /// 轻量测试辅助。无外部测试框架依赖。
    /// </summary>
    public static class TestRunner
    {
        private static int _passed;
        private static int _failed;
        private static string _currentSection = "";

        public static void StartSection(string name)
        {
            _currentSection = name;
            Console.WriteLine($"\n  [{name}]");
        }

        public static void Assert(bool condition, string message)
        {
            if (condition) { _passed++; }
            else { _failed++; Console.WriteLine($"    FAIL: {message}"); }
        }

        public static void AssertEqual<T>(T expected, T actual, string message) where T : IEquatable<T>
        {
            if (expected.Equals(actual)) { _passed++; }
            else { _failed++; Console.WriteLine($"    FAIL: {message} — expected {expected}, got {actual}"); }
        }

        public static void AssertEqual(int expected, int actual, string message)
        {
            if (expected == actual) { _passed++; }
            else { _failed++; Console.WriteLine($"    FAIL: {message} — expected {expected}, got {actual}"); }
        }

        public static void AssertEqual(bool expected, bool actual, string message)
        {
            if (expected == actual) { _passed++; }
            else { _failed++; Console.WriteLine($"    FAIL: {message} — expected {expected}, got {actual}"); }
        }

        public static void AssertEqual(double expected, double actual, double tolerance, string message)
        {
            if (System.Math.Abs(expected - actual) <= tolerance) { _passed++; }
            else { _failed++; Console.WriteLine($"    FAIL: {message} — expected ~{expected}, got {actual}"); }
        }

        public static void AssertApprox(double expected, double actual, double tolerance, string message)
        {
            if (System.Math.Abs(expected - actual) <= tolerance) { _passed++; }
            else { _failed++; Console.WriteLine($"    FAIL: {message} — expected ~{expected}, got {actual}"); }
        }

        public static (int passed, int failed) Summary()
        {
            return (_passed, _failed);
        }

        public static void Reset()
        {
            _passed = 0;
            _failed = 0;
        }
    }
}
