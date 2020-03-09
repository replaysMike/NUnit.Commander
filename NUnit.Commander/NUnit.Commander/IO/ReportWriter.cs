using AnyConsole;
using NUnit.Commander.Analysis;
using NUnit.Commander.Configuration;
using NUnit.Commander.Extensions;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NUnit.Commander.IO
{
    /// <summary>
    /// Handles writing of reports
    /// </summary>
    public class ReportWriter
    {
        private IExtendedConsole _console;
        private ApplicationConfiguration _configuration;
        private TestHistoryDatabaseProvider _testHistoryDatabaseProvider;
        private TestHistoryAnalyzer _testHistoryAnalyzer;
        private ReportContext _reportContext;
        private const char _headerChar = '═';
        private const char _headerBorderChar = '║';
        private const char _lineChar = '`';
        private readonly string _headerLine = new string(_headerChar, 36);
        private readonly string _lineSeparator = new string(_lineChar, Console.WindowWidth / 2);


        public ReportWriter(IExtendedConsole console, ApplicationConfiguration configuration, ReportContext reportContext)
        {
            _console = console;
            _configuration = configuration;
            _testHistoryDatabaseProvider = new TestHistoryDatabaseProvider(_configuration);
            _testHistoryAnalyzer = new TestHistoryAnalyzer(_configuration, _testHistoryDatabaseProvider);
            _reportContext = reportContext;
        }

        /// <summary>
        /// Write the final report to the console output
        /// </summary>
        /// <param name="allReports"></param>
        /// <param name="eventLog"></param>
        public void WriteFinalReport(ICollection<DataEvent> allReports, ICollection<EventEntry> eventLog)
        {
            _console.WriteLine();

            var passFail = new ColorTextBuilder();
            var totalDuration = TimeSpan.FromTicks(allReports.Sum(x => x.Duration.Ticks));
            var isPassed = allReports.All(x => x.TestStatus == TestStatus.Pass);
            if (_configuration.GenerateReportType.HasFlag(GenerateReportType.PassFail))
            {
                var allSuccess = allReports.Sum(x => x.Failed) == 0 && allReports.Sum(x => x.Passed) > 0;
                var anyFailure = allReports.Sum(x => x.Failed) > 0;
                var statusColor = Color.Red;
                var successColor = Color.DarkGreen;
                var failuresColor = Color.DarkRed;
                if (allSuccess)
                {
                    successColor = Color.Lime;
                    statusColor = Color.Lime;
                }
                if (allReports.Sum(x => x.Failed) > 0)
                    failuresColor = Color.Red;

                var testCount = allReports.Sum(x => x.TestCount);
                var passed = allReports.Sum(x => x.Passed);
                var failed = allReports.Sum(x => x.Failed);
                var warnings = allReports.Sum(x => x.Warnings);
                var asserts = allReports.Sum(x => x.Asserts);
                var inconclusive = allReports.Sum(x => x.Inconclusive);
                var errors = allReports.SelectMany(x => x.Report.TestReports.Select(t => !string.IsNullOrEmpty(t.ErrorMessage))).Count();
                var skipped = allReports.Sum(x => x.Skipped);
                var testResult = allReports.All(x => x.TestResult);

                WriteHeader(passFail, "Test Run Summary");

                passFail.Append($"  Overall result: ", Color.Gray);
                passFail.AppendLine(testResult ? "Passed" : "Failed", statusColor);

                passFail.Append($"  Duration: ", Color.Gray);
                passFail.Append($"{testCount} ", Color.White);
                passFail.Append($"tests run in ", Color.Gray);
                passFail.AppendLine($"{totalDuration.ToTotalElapsedTime()}", Color.Cyan);
                passFail.AppendLine("");

                passFail.Append($"  Test Count: ", Color.Gray);
                passFail.Append($"{testCount}", Color.White);

                passFail.Append($", Passed: ", Color.Gray);
                passFail.Append($"{passed}", successColor);
                passFail.Append($", Failed: ", Color.Gray);
                passFail.AppendLine($"{failed}", failuresColor);

                passFail.Append($"  Errors: ", Color.DarkGray);
                passFail.Append($"{errors}", Color.DarkRed);
                passFail.Append($", Warnings: ", Color.DarkGray);
                passFail.Append($"{warnings}", Color.LightGoldenrodYellow);
                passFail.Append($", Ignored: ", Color.DarkGray);
                passFail.AppendLine($"{skipped}", Color.Gray);

                passFail.Append($"  Asserts: ", Color.DarkGray);
                passFail.Append($"{asserts}", Color.Gray);
                passFail.Append($", Inconclusive: ", Color.DarkGray);
                passFail.AppendLine($"{inconclusive}", Color.Gray);

                passFail.AppendLine(Environment.NewLine);
            }

            var performance = new ColorTextBuilder();
            if (_configuration.GenerateReportType.HasFlag(GenerateReportType.Performance))
            {
                WriteHeader(performance, $"Top {_configuration.SlowestTestsCount} slowest tests");
                var slowestTests = eventLog
                    .Where(x => x.Event.Event == EventNames.EndTest)
                    .OrderByDescending(x => x.Event.Duration)
                    .Take(_configuration.SlowestTestsCount);
                foreach (var test in slowestTests)
                {
                    performance.Append($" \u2022 {test.Event.FullName.Replace(test.Event.TestName, "")}");
                    performance.Append($"{test.Event.TestName}", Color.White);
                    performance.AppendLine($" : {test.Event.Duration.ToElapsedTime()}", Color.Cyan);
                }
                performance.AppendLine(Environment.NewLine);
            }

            // output test errors
            var testOutput = new ColorTextBuilder();
            var showErrors = _configuration.GenerateReportType.HasFlag(GenerateReportType.Errors);
            var showStackTraces = _configuration.GenerateReportType.HasFlag(GenerateReportType.StackTraces);
            var showTestOutput = _configuration.GenerateReportType.HasFlag(GenerateReportType.TestOutput);
            var showTestAnalysis = _configuration.GenerateReportType.HasFlag(GenerateReportType.TestOutput);
            if (showErrors || showStackTraces || showTestOutput)
            {
                if (!isPassed)
                {
                    WriteHeader(testOutput, "FAILED TESTS", Color.Red);
                }

                var testIndex = 0;
                var failedTestCases = allReports
                    .SelectMany(x => x.Report.TestReports.Where(x => !x.TestResult));
                foreach (var test in failedTestCases)
                {
                    testIndex++;
                    var testIndexStr = $"{testIndex}) ";
                    testOutput.Append(testIndexStr, Color.DarkRed);
                    testOutput.AppendLine($"{test.TestName}", Color.Red);
                    testOutput.AppendLine($"{new string(' ', testIndexStr.Length)}{test.FullName.Replace($".{test.TestName}", "")}");
                    testOutput.AppendLine();

                    testOutput.Append($"  Duration: ", Color.DarkGray);
                    testOutput.AppendLine($"{test.Duration.ToElapsedTime()}", Color.Cyan);

                    if (showErrors && !string.IsNullOrEmpty(test.ErrorMessage))
                    {
                        testOutput.AppendLine($"  Error Output: ", Color.White);
                        testOutput.AppendLine(_lineSeparator, Color.DarkGray);
                        testOutput.AppendLine($"{test.ErrorMessage}", Color.DarkRed);
                        testOutput.AppendLine(_lineSeparator, Color.DarkGray);
                    }
                    if (showStackTraces && !string.IsNullOrEmpty(test.StackTrace))
                    {
                        testOutput.AppendLine($"  Stack Trace:", Color.White);
                        testOutput.AppendLine(_lineSeparator, Color.DarkGray);
                        testOutput.AppendLine($"{test.StackTrace}", Color.DarkRed);
                        testOutput.AppendLine(_lineSeparator, Color.DarkGray);
                    }
                    if (showTestOutput && !string.IsNullOrEmpty(test.TestOutput))
                    {
                        testOutput.AppendLine($"  Test Output: ", Color.White);
                        testOutput.AppendLine(_lineSeparator, Color.DarkGray);
                        testOutput.AppendLine($"{test.TestOutput}", Color.Gray);
                        testOutput.AppendLine(_lineSeparator, Color.DarkGray);
                    }
                    testOutput.AppendLine(Environment.NewLine);
                }
            }

            var historyReport = new HistoryReport();
            // analyze the historical data
            if (_configuration.HistoryAnalysisConfiguration.Enabled)
            {
                _testHistoryDatabaseProvider.LoadDatabase();
                var historyEntries = allReports
                    .SelectMany(x => x.Report.TestReports
                        .Select(y => new TestHistoryEntry(_reportContext.CommanderRunId.ToString(), x.TestRunId.ToString(), y)));
                // analyze before saving new results
                historyReport = _testHistoryAnalyzer.Analyze(historyEntries);
                // save results
                _testHistoryDatabaseProvider.AddTestHistoryRange(historyEntries);
                _testHistoryDatabaseProvider.SaveDatabase();
            }

            _console.WriteLine();
            _console.WriteLine(ColorTextBuilder.Create.Append($"╔{_headerLine}{_headerLine}", Color.Yellow).Append($"{_headerChar}", Color.FromArgb(128,128,0)).Append($"{_headerChar}", Color.FromArgb(64, 64, 0)).AppendLine($"{_headerChar}", Color.FromArgb(32, 32, 0))
                .AppendLine($"{_headerBorderChar}  NUnit.Commander Test Report", Color.Yellow));
            if (_reportContext.TestRunIds?.Any() == true)
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).AppendLine($"  Test Run Id(s): {string.Join(", ", _reportContext.TestRunIds)}"));
            if (_reportContext.Frameworks?.Any() == true)
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).AppendLine($"  Framework(s): {string.Join(", ", _reportContext.Frameworks)}"));
            if (_reportContext.FrameworkRuntimes?.Any() == true)
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).AppendLine($"  Framework Runtime(s): {string.Join(", ", _reportContext.FrameworkRuntimes)}"));
            _console.Write(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).Append($"  Test Start: {_reportContext.StartTime}"));
            _console.Write($"  Test End: {_reportContext.EndTime}");
            _console.WriteLine($"  Total Duration: {_reportContext.EndTime.Subtract(_reportContext.StartTime)}");
            _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).AppendLine($"  Settings:"));
            _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).AppendLine($"    Runtime={totalDuration}"));
            if (_console.IsOutputRedirected)
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).AppendLine($"    LogMode=Enabled"));
            else
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).AppendLine($"    LogMode=Disabled"));
            _console.WriteLine(ColorTextBuilder.Create.Append($"╚{_headerLine}{_headerLine}", Color.Yellow).Append($"{_headerChar}", Color.FromArgb(128, 128, 0)).Append($"{_headerChar}", Color.FromArgb(64, 64, 0)).AppendLine($"{_headerChar}", Color.FromArgb(32, 32, 0)));

            // write large PASSED / FAILED ascii art
            if (isPassed)
                _console.WriteAscii(ColorTextBuilder.Create.Append("PASSED", Color.Lime));
            else
                _console.WriteAscii(ColorTextBuilder.Create.Append("FAILED", Color.Red));

            if (performance.Length > 0)
                _console.WriteLine(performance);
            if (testOutput.Length > 0)
                _console.WriteLine(testOutput);
            if (showTestAnalysis)
            {
                var testAnalysisOutput = new ColorTextBuilder();
                WriteHeader(testAnalysisOutput, "Historical Analysis Report");
                _console.WriteLine(testAnalysisOutput);
                // write the analysis report
                _console.WriteLine(historyReport.BuildReport(_testHistoryAnalyzer.TotalDataPoints));
            }
            if (passFail.Length > 0)
                _console.WriteLine(passFail);
        }

        private void WriteHeader(ColorTextBuilder builder, string str, Color? color = null)
        {
            builder.AppendLine($"╔{new string(_headerChar, str.Length + 4)}╗", color ?? Color.Yellow);
            builder.AppendLine($"{_headerBorderChar}  {str}  {_headerBorderChar}", color ?? Color.Yellow);
            builder.AppendLine($"╚{new string(_headerChar, str.Length + 4)}╝", color ?? Color.Yellow);
        }
    }
}
