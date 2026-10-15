using System;
using System.Collections.Generic;

namespace HideBar.Tests
{
    // A small test runner, so the tests build with the same compiler as the app and need no
    // packages. Run them with test.cmd.
    static class TestRunner
    {
        static readonly List<KeyValuePair<string, Action>> tests = new List<KeyValuePair<string, Action>>();

        public static void Add(string name, Action test)
        {
            tests.Add(new KeyValuePair<string, Action>(name, test));
        }

        static int Main()
        {
            RectTests.Register();
            SettingsFormatTests.Register();
            TaskbarControllerTests.Register();

            int failed = 0;
            foreach (KeyValuePair<string, Action> test in tests)
            {
                try
                {
                    test.Value();
                    Console.WriteLine("  ok    " + test.Key);
                }
                catch (Exception e)
                {
                    failed++;
                    Console.WriteLine("  FAIL  " + test.Key);
                    Console.WriteLine("        " + e.Message);
                }
            }

            Console.WriteLine();
            Console.WriteLine(failed == 0
                ? "All " + tests.Count + " tests passed."
                : failed + " of " + tests.Count + " tests failed.");
            return failed == 0 ? 0 : 1;
        }
    }

    static class Assert
    {
        public static void Equal<T>(T expected, T actual, string what)
        {
            if (!Equals(expected, actual)) throw new Exception(what + ": expected " + expected + ", got " + actual);
        }

        public static void True(bool condition, string what)
        {
            if (!condition) throw new Exception(what);
        }

        public static void False(bool condition, string what)
        {
            if (condition) throw new Exception(what);
        }
    }
}
