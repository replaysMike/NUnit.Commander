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
        public static int MaxWidth = 160;

        public static ColorTextBuilder GetPrettyTestName(string fullName, Color? pathColor = null, Color? testNameColor = null, Color? argsColor = null, int maxTestCaseArgumentLength = 20)
        {
            var maxWidth = MaxWidth;
            if (!Console.IsOutputRedirected)
                maxWidth = Console.WindowWidth - 26;
            var test = fullName;
            var testPath = fullName;
            var testCaseArgs = string.Empty;
            try
            {

                var argsIndex = test.IndexOf('(');
                var lastIndex = test.LastIndexOf('.', argsIndex > 0 ? argsIndex : test.Length - 1);
                if (lastIndex >= 0)
                {
                    // if a path is detected (before the arguments) separate it out
                    test = fullName.Substring(lastIndex + 1, test.Length - lastIndex - 1);
                    testPath = fullName.Substring(0, lastIndex + 1);
                }
                else
                    testPath = string.Empty;

                argsIndex = test.IndexOf('(');
                if (argsIndex > 0)
                {
                    var argsEndIndex = test.LastIndexOf(")");
                    if (argsIndex > 0 && argsEndIndex > 0)
                    {
                        testCaseArgs = test.Substring(argsIndex, argsEndIndex - argsIndex) + ")";
                        // remove args from test name
                        test = test.Substring(0, argsIndex);
                    }
                }
                // truncate total size
                if (testPath.Length + test.Length + testCaseArgs.Length > maxWidth)
                {
                    // path truncates first
                    if (testPath.Length + test.Length + testCaseArgs.Length > maxWidth)
                    {
                        var maxLength = Math.Max(0, testPath.Length - (testPath.Length + test.Length + testCaseArgs.Length - maxWidth) - 3);
                        if (maxLength > 0)
                            testPath = testPath.Substring(0, maxLength) + "...";
                        else
                            testPath = string.Empty;
                    }
                    // test args truncates second
                    if (testPath.Length + test.Length + testCaseArgs.Length > maxWidth)
                    {
                        var maxLength = Math.Max(0, testCaseArgs.Length - (testPath.Length + test.Length + testCaseArgs.Length - maxWidth) - 3);
                        if (maxLength > 0)
                            testCaseArgs = testCaseArgs.Substring(0, maxLength) + "...";
                        else
                            testCaseArgs = string.Empty;
                    }
                    // test name truncates last
                    if (testPath.Length + test.Length + testCaseArgs.Length > maxWidth)
                    {
                        var maxLength = Math.Max(0, test.Length - (testPath.Length + test.Length + testCaseArgs.Length - maxWidth) - 3);
                        if (maxLength > 0)
                            test = test.Substring(0, maxLength) + "...";
                        else
                            test = string.Empty;
                    }
                }

                return ColorTextBuilder.Create
                        .AppendIf(testPath?.Length > 0, testPath, pathColor ?? Color.DarkSlateGray)
                        .Append(test, testNameColor ?? Color.Gray)
                        .AppendIf(!string.IsNullOrEmpty(testCaseArgs), testCaseArgs, argsColor ?? Color.DarkSlateGray);
            }
            catch (Exception ex)
            {
                return ColorTextBuilder.Create.Append($"{fullName} - ERROR: {ex.Message}");
            }
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
