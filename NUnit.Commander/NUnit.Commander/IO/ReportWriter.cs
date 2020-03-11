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
        private RunContext _runContext;
        private const char _headerChar = '═';
        private const char _headerBorderChar = '║';
        private const char _lineChar = '`';
        private readonly string _headerLine = new string(_headerChar, 36);
        private readonly string _lineSeparator = new string(_lineChar, Console.WindowWidth / 2);


        public ReportWriter(IExtendedConsole console, ApplicationConfiguration configuration, RunContext runContext)
        {
            _console = console;
            _configuration = configuration;
            _testHistoryDatabaseProvider = new TestHistoryDatabaseProvider(_configuration);
            _testHistoryAnalyzer = new TestHistoryAnalyzer(_configuration, _testHistoryDatabaseProvider);
            _runContext = runContext;
        }

        /// <summary>
        /// Write the final report to the console output
        /// </summary>
        /// <param name="allReports"></param>
        /// <param name="eventLog"></param>
        public void WriteFinalReport()
        {
            if (!_console.IsOutputRedirected)
                _console.Clear();

            var commanderIdMap = _runContext.Runs.ToDictionary(key => key.Key.CommanderRunId, value => value.Value.Select(y => y.TestRunId).ToList());
            var allReports = _runContext.Runs.SelectMany(x => x.Value);

            var passFail = new ColorTextBuilder();
            var totalDuration = TimeSpan.FromTicks(allReports.Sum(x => x.Duration.Ticks));
            var isPassed = allReports
                .SelectMany(x => x.Report.TestReports)
                .Count(x => x.TestStatus == TestStatus.Fail) == 0;
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
                var totalRuns = allReports.GroupBy(x => x.RunNumber).Count();

                WriteHeader(passFail, "Test Run Summary");

                passFail.Append($"  Overall result: ", Color.Gray);
                passFail.AppendLine(isPassed ? "Passed" : "Failed", statusColor);

                passFail.Append($"  Duration: ", Color.Gray);
                passFail.Append($"{testCount:N0} ", Color.White);
                passFail.Append($"tests run in ", Color.Gray);
                passFail.AppendLine($"{totalDuration.ToTotalElapsedTime()}", Color.Cyan);
                passFail.AppendLine("");

                passFail.Append($"  Test Runs: ", Color.Gray);
                passFail.AppendLine($"{totalRuns}", Color.White);

                passFail.Append($"  Test Count: ", Color.Gray);
                passFail.Append($"{testCount:N0}", Color.White);

                passFail.Append($", Passed: ", Color.Gray);
                passFail.Append($"{passed:N0}", successColor);
                passFail.Append($", Failed: ", Color.Gray);
                passFail.AppendLine($"{failed:N0}", failuresColor);

                passFail.Append($"  Errors: ", Color.DarkSlateGray);
                passFail.Append($"{errors:N0}", Color.DarkRed);
                passFail.Append($", Warnings: ", Color.DarkSlateGray);
                passFail.Append($"{warnings:N0}", Color.LightGoldenrodYellow);
                passFail.Append($", Ignored: ", Color.DarkSlateGray);
                passFail.AppendLine($"{skipped}", Color.Gray);

                passFail.Append($"  Asserts: ", Color.DarkSlateGray);
                passFail.Append($"{asserts:N0}", Color.Gray);
                passFail.Append($", Inconclusive: ", Color.DarkSlateGray);
                passFail.AppendLine($"{inconclusive:N0}", Color.Gray);

                passFail.AppendLine(Environment.NewLine);
            }

            // build the same type of summary, but for each individual run
            var passFailByRun = new ColorTextBuilder();
            if (_runContext.Runs.Count > 1)
            {
                var runNumber = 0;
                foreach(var run in _runContext.Runs)
                {
                    runNumber++;

                    var allSuccess = run.Value.Sum(x => x.Failed) == 0 && run.Value.Sum(x => x.Passed) > 0;
                    var anyFailure = run.Value.Sum(x => x.Failed) > 0;
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
                    
                    var testCount = run.Value.Sum(x => x.TestCount);
                    var passed = run.Value.Sum(x => x.Passed);
                    var failed = run.Value.Sum(x => x.Failed);
                    var warnings = run.Value.Sum(x => x.Warnings);
                    var asserts = run.Value.Sum(x => x.Asserts);
                    var inconclusive = run.Value.Sum(x => x.Inconclusive);
                    var errors = run.Value.SelectMany(x => x.Report.TestReports.Select(t => !string.IsNullOrEmpty(t.ErrorMessage))).Count();
                    var skipped = run.Value.Sum(x => x.Skipped);
                    var totalRuns = run.Value.GroupBy(x => x.RunNumber).Count();

                    WriteHeader(passFailByRun, $"Test Run #{runNumber} Summary", Color.FromArgb(128,128,0));

                    passFailByRun.Append($"  Overall result: ", Color.Gray);
                    passFailByRun.AppendLine(isPassed ? "Passed" : "Failed", statusColor);

                    passFailByRun.Append($"  Duration: ", Color.Gray);
                    passFailByRun.Append($"{testCount:N0} ", Color.White);
                    passFailByRun.Append($"tests run in ", Color.Gray);
                    passFailByRun.AppendLine($"{run.Key.EndTime.Subtract(run.Key.StartTime).ToTotalElapsedTime()}", Color.Cyan);
                    passFailByRun.AppendLine("");

                    passFailByRun.Append($"  Test Runs: ", Color.Gray);
                    passFailByRun.AppendLine($"{totalRuns}", Color.White);

                    passFailByRun.Append($"  Test Count: ", Color.Gray);
                    passFailByRun.Append($"{testCount:N0}", Color.White);

                    passFailByRun.Append($", Passed: ", Color.Gray);
                    passFailByRun.Append($"{passed:N0}", successColor);
                    passFailByRun.Append($", Failed: ", Color.Gray);
                    passFailByRun.AppendLine($"{failed:N0}", failuresColor);

                    passFailByRun.Append($"  Errors: ", Color.DarkSlateGray);
                    passFailByRun.Append($"{errors:N0}", Color.DarkRed);
                    passFailByRun.Append($", Warnings: ", Color.DarkSlateGray);
                    passFailByRun.Append($"{warnings:N0}", Color.LightGoldenrodYellow);
                    passFailByRun.Append($", Ignored: ", Color.DarkSlateGray);
                    passFailByRun.AppendLine($"{skipped}", Color.Gray);

                    passFailByRun.Append($"  Asserts: ", Color.DarkSlateGray);
                    passFailByRun.Append($"{asserts:N0}", Color.Gray);
                    passFailByRun.Append($", Inconclusive: ", Color.DarkSlateGray);
                    passFailByRun.AppendLine($"{inconclusive:N0}", Color.Gray);

                    passFailByRun.AppendLine(Environment.NewLine);
                }
            }

            var performance = new ColorTextBuilder();
            if (_configuration.GenerateReportType.HasFlag(GenerateReportType.Performance))
            {
                WriteHeader(performance, $"Top {_configuration.SlowestTestsCount} slowest tests");
                var allTestsByName = _runContext.Runs
                    .SelectMany(x => x.Key.EventEntries)
                    .GroupBy(x => x.Event.TestName);
                var slowestTests = allTestsByName
                    .SelectMany(x => x.Select(y => y.Event))
                    .Where(x => x.Event == EventNames.EndTest)
                    .OrderByDescending(x => x.Duration)
                    .GroupBy(x => x.TestName)
                    .Take(_configuration.SlowestTestsCount);
                foreach (var test in slowestTests)
                {
                    performance.Append($" \u2022 {test.FirstOrDefault().FullName.Replace(test.FirstOrDefault().TestName, "")}");
                    performance.Append($"{test.FirstOrDefault().TestName}", Color.White);
                    performance.AppendLine($" : {test.FirstOrDefault().Duration.ToElapsedTime()}", Color.Cyan);
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
                    .GroupBy(x => x.RunNumber)
                    .SelectMany(x => x.ToDictionary(y => y.RunNumber, value => value.Report.TestReports.Where(x => x.TestStatus == TestStatus.Fail)));
                foreach (var testGroup in failedTestCases)
                {
                    var runNumber = testGroup.Key;
                    foreach (var test in testGroup.Value)
                    {
                        testIndex++;
                        var testIndexStr = $"#{runNumber}-{testIndex}) ";
                        testOutput.Append(testIndexStr, Color.DarkRed);
                        testOutput.AppendLine($"{test.TestName}", Color.Red);
                        testOutput.AppendLine($"{new string(' ', testIndexStr.Length)}{test.FullName.Replace($".{test.TestName}", "")}");
                        testOutput.AppendLine($"{new string(' ', testIndexStr.Length)}{test.RuntimeVersion}", Color.DarkCyan);
                        testOutput.AppendLine();

                        testOutput.Append($"  Duration: ", Color.DarkSlateGray);
                        testOutput.AppendLine($"{test.Duration.ToElapsedTime()}", Color.Cyan);

                        if (showErrors && !string.IsNullOrEmpty(test.ErrorMessage))
                        {
                            testOutput.AppendLine($"  Error Output: ", Color.White);
                            testOutput.AppendLine(_lineSeparator, Color.DarkSlateGray);
                            testOutput.AppendLine($"{test.ErrorMessage}", Color.DarkRed);
                            testOutput.AppendLine(_lineSeparator, Color.DarkSlateGray);
                        }
                        if (showStackTraces && !string.IsNullOrEmpty(test.StackTrace))
                        {
                            testOutput.AppendLine($"  Stack Trace:", Color.White);
                            testOutput.AppendLine(_lineSeparator, Color.DarkSlateGray);
                            testOutput.AppendLine($"{test.StackTrace}", Color.DarkRed);
                            testOutput.AppendLine(_lineSeparator, Color.DarkSlateGray);
                        }
                        if (showTestOutput && !string.IsNullOrEmpty(test.TestOutput))
                        {
                            testOutput.AppendLine($"  Test Output: ", Color.White);
                            testOutput.AppendLine(_lineSeparator, Color.DarkSlateGray);
                            testOutput.AppendLine($"{test.TestOutput}", Color.Gray);
                            testOutput.AppendLine(_lineSeparator, Color.DarkSlateGray);
                        }
                        testOutput.AppendLine(Environment.NewLine);
                    }
                }
            }

            var historyReport = new HistoryReport();
            // analyze the historical data
            if (_configuration.HistoryAnalysisConfiguration.Enabled)
            {
                _testHistoryDatabaseProvider.LoadDatabase();
                var historyEntries = allReports
                    .SelectMany(x => x.Report.TestReports
                        .Where(y => y.TestStatus != TestStatus.Skipped)
                        .Select(y => new TestHistoryEntry(commanderIdMap.Where(z => z.Value.Contains(x.TestRunId)).Select(z => z.Key).FirstOrDefault().ToString(), x.TestRunId.ToString(), y)));
                // analyze before saving new results
                historyReport = _testHistoryAnalyzer.Analyze(historyEntries);
                // save results
                _testHistoryDatabaseProvider.AddTestHistoryRange(historyEntries);
                _testHistoryDatabaseProvider.SaveDatabase();
            }

            _console.WriteLine();
            _console.WriteLine(ColorTextBuilder.Create.Append($"╔{_headerLine}{_headerLine}", Color.Yellow).Append($"{_headerChar}", Color.FromArgb(128,128,0)).Append($"{_headerChar}", Color.FromArgb(64, 64, 0)).AppendLine($"{_headerChar}", Color.FromArgb(32, 32, 0))
                .AppendLine($"{_headerBorderChar}  NUnit.Commander Test Report", Color.Yellow));
            var testRunIds = allReports.GroupBy(x => x.TestRunId).Select(x => x.Key);
            if (testRunIds?.Any() == true)
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).AppendLine($"  Test Run Id(s): {string.Join(", ", testRunIds)}"));
            var frameworks = _runContext.Runs.SelectMany(x => x.Key.Frameworks).Distinct();
            if (frameworks.Any() == true)
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).AppendLine($"  Framework(s): {string.Join(", ", frameworks)}"));
            var frameworkRuntimes = _runContext.Runs.SelectMany(x => x.Key.FrameworkRuntimes).Distinct();
            if (frameworkRuntimes.Any() == true)
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).AppendLine($"  Framework Runtime(s): {string.Join(", ", frameworkRuntimes)}"));
            var startTime = _runContext.Runs.Select(x => x.Key.StartTime).OrderBy(x => x).FirstOrDefault();
            var endTime = _runContext.Runs.Select(x => x.Key.EndTime).OrderByDescending(x => x).FirstOrDefault();
            _console.Write(ColorTextBuilder.Create.Append($"{_headerBorderChar}", Color.Yellow).Append($"  Test Start: {startTime}"));
            _console.Write($"  Test End: {endTime}");
            _console.WriteLine($"  Total Duration: {endTime.Subtract(startTime)}");
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
            if (passFailByRun.Length > 0)
                _console.WriteLine(passFailByRun);
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
