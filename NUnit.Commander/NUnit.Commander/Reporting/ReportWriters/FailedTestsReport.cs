using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Extensions;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NUnit.Commander.Reporting.ReportWriters
{
    public class FailedTestsReport : ReportBase
    {
        public FailedTestsReport(ApplicationConfiguration configuration, IExtendedConsole console, RunContext runContext, ColorManager colorScheme) : base(configuration, console, runContext, colorScheme) { }

        public override ColorTextBuilder Write(object parameters = null)
        {
            var allReports = (IEnumerable<DataEvent>)parameters;
            var builder = new ColorTextBuilder();
            var lineSeparator = new string(UTF8Constants.HorizontalLine, !_console.IsOutputRedirected ? (Console.WindowWidth - 1) : DefaultBorderWidth);

            var showErrors = _configuration.GenerateReportType.HasFlag(GenerateReportType.Errors);
            var showStackTraces = _configuration.GenerateReportType.HasFlag(GenerateReportType.StackTraces);
            var showTestOutput = _configuration.GenerateReportType.HasFlag(GenerateReportType.TestOutput);
            if (showErrors || showStackTraces || showTestOutput)
            {
                var isPassed = allReports
                    .SelectMany(x => x.Report.TestReports)
                    .Count(x => x.TestStatus == TestStatus.Fail) == 0;

                if (!isPassed)
                    WriteRoundBox(builder, "FAILED TESTS", 0, _colorScheme.Error);

                var testIndex = 0;
                var groupedByRun = allReports
                    .GroupBy(x => x.RunNumber);
                var failedTestCases = groupedByRun
                    .ToDictionary(x => x.Key, value => value.SelectMany(y => y.Report.TestReports.Where(z => z.TestStatus == TestStatus.Fail)));
                var lineSeparatorColor = _colorScheme.RaisedBackground;
                foreach (var testGroup in failedTestCases)
                {
                    var runNumber = testGroup.Key;
                    foreach (var test in testGroup.Value)
                    {
                        testIndex++;

                        // write the failed test header
                        var testIndexStr = $"#{testIndex}) ";
                        if (failedTestCases.Count() > 1)
                            testIndexStr = $"#{runNumber}-{testIndex}) ";
                        builder.Append(testIndexStr, _colorScheme.DarkError, _colorScheme.RaisedBackground);

                        // write the test name only
                        builder.Append(test.TestName, _colorScheme.Error, _colorScheme.RaisedBackground);
                        if (!_console.IsOutputRedirected)
                            builder.AppendLine($"{DisplayUtil.Pad(Console.WindowWidth - testIndexStr.Length - test.TestName.Length - 1)}", _colorScheme.Error, _colorScheme.RaisedBackground);
                        else
                            builder.AppendLine();

                        // write the test path
                        var fullName = $"{DisplayUtil.Pad(testIndexStr.Length)}{test.FullName.Replace($".{test.TestName}", "")}";
                        builder.Append(fullName, _colorScheme.DarkDuration, _colorScheme.RaisedBackground);
                        if (!_console.IsOutputRedirected)
                            builder.AppendLine($"{DisplayUtil.Pad(Console.WindowWidth - fullName.Length - 1)}", _colorScheme.Error, _colorScheme.RaisedBackground);
                        else
                            builder.AppendLine();

                        var runtimeVersion = $"{DisplayUtil.Pad(testIndexStr.Length)}{test.RuntimeVersion}";
                        builder.Append($"{runtimeVersion}", _colorScheme.DarkDefault, _colorScheme.Background ?? Color.Black);
                        if (!_console.IsOutputRedirected)
                            builder.AppendLine($"{DisplayUtil.Pad(Console.WindowWidth - runtimeVersion.Length)}", _colorScheme.Error, _colorScheme.Background ?? Color.Black);
                        else
                            builder.AppendLine();

                        builder.AppendLine();

                        builder.Append($"  Duration ", _colorScheme.DarkDefault);
                        builder.AppendLine($"{test.Duration.ToElapsedTime()}", _colorScheme.Duration);

                        if (showErrors && !string.IsNullOrEmpty(test.ErrorMessage))
                        {
                            builder.AppendLine($"  Error Output ", _colorScheme.Bright);
                            builder.AppendLine(lineSeparator, lineSeparatorColor);
                            builder.AppendLine($"{test.ErrorMessage}", _colorScheme.DarkDefault);
                            builder.AppendLine(lineSeparator, lineSeparatorColor);
                        }
                        if (showStackTraces && !string.IsNullOrEmpty(test.StackTrace))
                        {
                            builder.AppendLine($"  Stack Trace:", _colorScheme.Bright);
                            builder.AppendLine(lineSeparator, lineSeparatorColor);
                            builder.Append(StackTracePrettify.Format(test.StackTrace, _colorScheme));
                            builder.AppendLine();
                            builder.AppendLine(lineSeparator, lineSeparatorColor);
                        }
                        if (showTestOutput && !string.IsNullOrEmpty(test.TestOutput))
                        {
                            builder.AppendLine($"  Test Output: ", _colorScheme.Bright);
                            builder.AppendLine(lineSeparator, lineSeparatorColor);
                            builder.AppendLine($"{test.TestOutput}", _colorScheme.Default);
                            builder.AppendLine(lineSeparator, lineSeparatorColor);
                        }
                        builder.AppendLine(Environment.NewLine);
                    }
                }
            }
            return builder;
        }
    }
}
