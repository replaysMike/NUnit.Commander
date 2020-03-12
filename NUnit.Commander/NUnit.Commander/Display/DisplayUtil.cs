using AnyConsole;
using System;
using System.Drawing;

namespace NUnit.Commander.Display
{
    /// <summary>
    /// Display utilities
    /// </summary>
    public static class DisplayUtil
    {
        public static ColorTextBuilder GetPrettyTestName(string testName, int maxTestCaseArgumentLength = 10)
        {
            var test = testName;
            var testPath = testName;
            var testCaseArgs = string.Empty;

            var argsIndex = test.IndexOf('(');
            var lastIndex = test.LastIndexOf('.', argsIndex > 0 ? argsIndex : test.Length - 1);
            if (lastIndex >= 0)
            {
                // if a path is detected (before the arguments) separate it out
                test = testName.Substring(lastIndex + 1, test.Length - lastIndex - 1);
                testPath = testName.Substring(0, lastIndex + 1);
            }
            else
                testPath = string.Empty;

            argsIndex = test.IndexOf('(');
            if (argsIndex > 0)
            {
                // strip the test case arguments if it won't fit on screen
                var argsEndIndex = test.LastIndexOf(")");
                if (argsIndex > 0 && argsEndIndex > 0)
                {
                    var maxLength = maxTestCaseArgumentLength;
                    testCaseArgs = test.Substring(argsIndex, argsEndIndex - argsIndex);
                    if (testCaseArgs.Length > maxLength)
                    {
                        testCaseArgs = testCaseArgs.Substring(0, maxLength) + "...";
                        if (testCaseArgs.Contains("\"")) testCaseArgs += "\"";
                    }
                    testCaseArgs += ")";
                    // remove args from test name
                    test = test.Substring(0, argsIndex);
                }
            }
            // truncate total size
            if (testPath.Length + test.Length + testCaseArgs.Length > Console.WindowWidth - 30)
            {
                testCaseArgs = string.Empty;
                if (testPath.Length + test.Length + testCaseArgs.Length > Console.WindowWidth - 30)
                    test = test.Substring(0, Console.WindowWidth - 30) + "...";
            }

            return ColorTextBuilder.Create
                    .AppendIf(testPath?.Length > 0, testPath, Color.DarkSlateGray)
                    .Append(test)
                    .AppendIf(!string.IsNullOrEmpty(testCaseArgs), testCaseArgs, Color.DarkSlateGray);
        }

        private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string GetFriendlyBytes(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + GetFriendlyBytes(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
    }
}
