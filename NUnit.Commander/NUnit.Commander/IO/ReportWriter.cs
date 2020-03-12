using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Extensions;
using NUnit.Commander.Models;
using System;
using System.Drawing;
using System.Linq;

namespace NUnit.Commander.IO
{
    /// <summary>
    /// Handles writing of reports
    /// </summary>
    public class ReportWriter
    {
        private Colors _colorScheme;
        private IExtendedConsole _console;
        private ApplicationConfiguration _configuration;
        private RunContext _runContext;
        private const char _headerChar = '═';
        private const char _headerBorderChar = '║';
        private const char _lineChar = '`';
        private readonly string _headerLine = new string(_headerChar, 36);
        private readonly string _lineSeparator = new string(_lineChar, Console.WindowWidth / 2);


        public ReportWriter(IExtendedConsole console, Colors colorScheme, ApplicationConfiguration configuration, RunContext runContext)
        {
            _console = console;
            _colorScheme = colorScheme;
            _configuration = configuration;
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


            // ***********************
            // Total Run Summary
            // ***********************
            var passFail = new ColorTextBuilder();
            var totalDuration = TimeSpan.FromTicks(allReports.Sum(x => x.Duration.Ticks));
            var isPassed = allReports
                .SelectMany(x => x.Report.TestReports)
                .Count(x => x.TestStatus == TestStatus.Fail) == 0;
            if (_configuration.GenerateReportType.HasFlag(GenerateReportType.PassFail))
            {
                var allSuccess = allReports.Sum(x => x.Failed) == 0 && allReports.Sum(x => x.Passed) > 0;
                var anyFailure = allReports.Sum(x => x.Failed) > 0;
                var statusColor = _colorScheme.Error;
                var successColor = _colorScheme.DarkSuccess;
                var failuresColor = _colorScheme.DarkError;
                if (allSuccess)
                {
                    successColor = _colorScheme.Success;
                    statusColor = _colorScheme.Success;
                }
                if (allReports.Sum(x => x.Failed) > 0)
                    failuresColor = _colorScheme.Error;

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

                passFail.Append($"  Overall result: ", _colorScheme.Default);
                passFail.AppendLine(isPassed ? "Passed" : "Failed", statusColor);

                passFail.Append($"  Duration: ", _colorScheme.Default);
                passFail.Append($"{testCount:N0} ", _colorScheme.Bright);
                passFail.Append($"tests run in ", _colorScheme.Default);
                passFail.AppendLine($"{totalDuration.ToTotalElapsedTime()}", _colorScheme.Duration);
                passFail.AppendLine("");

                passFail.Append($"  Test Runs: ", _colorScheme.Default);
                passFail.AppendLine($"{totalRuns}", _colorScheme.Bright);

                passFail.Append($"  Test Count: ", _colorScheme.Default);
                passFail.Append($"{testCount:N0}", _colorScheme.Bright);

                passFail.Append($", Passed: ", _colorScheme.Default);
                passFail.Append($"{passed:N0}", successColor);
                passFail.Append($", Failed: ", _colorScheme.Default);
                passFail.AppendLine($"{failed:N0}", failuresColor);

                passFail.Append($"  Errors: ", _colorScheme.DarkDefault);
                passFail.Append($"{errors:N0}", _colorScheme.DarkError);
                passFail.Append($", Warnings: ", _colorScheme.DarkDefault);
                passFail.Append($"{warnings:N0}", _colorScheme.DarkHighlight);
                passFail.Append($", Ignored: ", _colorScheme.DarkDefault);
                passFail.AppendLine($"{skipped}", _colorScheme.Default);

                passFail.Append($"  Asserts: ", _colorScheme.DarkDefault);
                passFail.Append($"{asserts:N0}", _colorScheme.Default);
                passFail.Append($", Inconclusive: ", _colorScheme.DarkDefault);
                passFail.AppendLine($"{inconclusive:N0}", _colorScheme.Default);

                passFail.Append($"  Peak Cpu: ", _colorScheme.DarkDefault);
                passFail.Append($"{(_runContext.Runs.Any() ? _runContext.Runs.Max(x => x.Key.Performance.PeakCpuUsed) : 0):N0}%", _colorScheme.Default);
                passFail.Append($", Median: ", _colorScheme.DarkDefault);
                passFail.AppendLine($"{(_runContext.Runs.Any() ? _runContext.Runs.Median(x => x.Key.Performance.MedianCpuUsed) : 0):N0}%", _colorScheme.Default);

                passFail.Append($"  Peak Memory: ", _colorScheme.DarkDefault);
                passFail.Append($"{DisplayUtil.GetFriendlyBytes((long)_runContext.Runs.Max(x => x.Key.Performance.PeakMemoryUsed))}", _colorScheme.Default);
                passFail.Append($", Median: ", _colorScheme.DarkDefault);
                passFail.AppendLine($"{DisplayUtil.GetFriendlyBytes((long)_runContext.Runs.Median(x => x.Key.Performance.MedianMemoryUsed))}", _colorScheme.Default);

                passFail.Append($"  Peak Disk Time: ", _colorScheme.DarkDefault);
                passFail.Append($"{(_runContext.Runs.Any() ? _runContext.Runs.Max(x => x.Key.Performance.PeakDiskTime) : 0):N0}%", _colorScheme.Default);
                passFail.Append($", Median: ", _colorScheme.DarkDefault);
                passFail.AppendLine($"{(_runContext.Runs.Any() ? _runContext.Runs.Median(x => x.Key.Performance.MedianDiskTime) : 0):N0}%", _colorScheme.Default);

                passFail.AppendLine(Environment.NewLine);
            }

            // ***********************
            // Multiple Run Summary
            // ***********************
            var passFailByRun = new ColorTextBuilder();
            if (_runContext.Runs.Count > 1)
            {
                var runNumber = 0;
                foreach(var run in _runContext.Runs)
                {
                    runNumber++;

                    var allSuccess = run.Value.Sum(x => x.Failed) == 0 && run.Value.Sum(x => x.Passed) > 0;
                    var anyFailure = run.Value.Sum(x => x.Failed) > 0;
                    var statusColor = _colorScheme.Error;
                    var successColor = _colorScheme.DarkSuccess;
                    var failuresColor = _colorScheme.DarkError;
                    if (allSuccess)
                    {
                        successColor = _colorScheme.DarkSuccess;
                        statusColor = _colorScheme.DarkSuccess;
                    }
                    if (allReports.Sum(x => x.Failed) > 0)
                        failuresColor = _colorScheme.DarkError;
                    
                    var testCount = run.Value.Sum(x => x.TestCount);
                    var passed = run.Value.Sum(x => x.Passed);
                    var failed = run.Value.Sum(x => x.Failed);
                    var warnings = run.Value.Sum(x => x.Warnings);
                    var asserts = run.Value.Sum(x => x.Asserts);
                    var inconclusive = run.Value.Sum(x => x.Inconclusive);
                    var errors = run.Value.SelectMany(x => x.Report.TestReports.Select(t => !string.IsNullOrEmpty(t.ErrorMessage))).Count();
                    var skipped = run.Value.Sum(x => x.Skipped);
                    var totalRuns = run.Value.GroupBy(x => x.RunNumber).Count();

                    WriteHeader(passFailByRun, $"Test Run #{runNumber} Summary", _colorScheme.DarkHighlight);

                    passFailByRun.Append($"  Overall result: ", _colorScheme.Default);
                    passFailByRun.AppendLine(isPassed ? "Passed" : "Failed", statusColor);

                    passFailByRun.Append($"  Duration: ", _colorScheme.Default);
                    passFailByRun.Append($"{testCount:N0} ", _colorScheme.Bright);
                    passFailByRun.Append($"tests run in ", _colorScheme.Default);
                    passFailByRun.AppendLine($"{run.Key.EndTime.Subtract(run.Key.StartTime).ToTotalElapsedTime()}", _colorScheme.Duration);
                    passFailByRun.AppendLine("");

                    passFailByRun.Append($"  Test Runs: ", _colorScheme.Default);
                    passFailByRun.AppendLine($"{totalRuns}", _colorScheme.Bright);

                    passFailByRun.Append($"  Test Count: ", _colorScheme.Default);
                    passFailByRun.Append($"{testCount:N0}", _colorScheme.Bright);

                    passFailByRun.Append($", Passed: ", _colorScheme.Default);
                    passFailByRun.Append($"{passed:N0}", successColor);
                    passFailByRun.Append($", Failed: ", _colorScheme.Default);
                    passFailByRun.AppendLine($"{failed:N0}", failuresColor);

                    passFailByRun.Append($"  Errors: ", _colorScheme.DarkDefault);
                    passFailByRun.Append($"{errors:N0}", _colorScheme.DarkError);
                    passFailByRun.Append($", Warnings: ", _colorScheme.DarkDefault);
                    passFailByRun.Append($"{warnings:N0}", _colorScheme.DarkHighlight);
                    passFailByRun.Append($", Ignored: ", _colorScheme.DarkDefault);
                    passFailByRun.AppendLine($"{skipped}", _colorScheme.Default);

                    passFailByRun.Append($"  Asserts: ", _colorScheme.DarkDefault);
                    passFailByRun.Append($"{asserts:N0}", _colorScheme.Default);
                    passFailByRun.Append($", Inconclusive: ", _colorScheme.DarkDefault);
                    passFailByRun.AppendLine($"{inconclusive:N0}", _colorScheme.Default);

                    passFailByRun.Append($"  Peak Cpu: ", _colorScheme.DarkDefault);
                    passFailByRun.Append($"{run.Key.Performance.PeakCpuUsed:N0}%", _colorScheme.Default);
                    passFailByRun.Append($", Median: ", _colorScheme.DarkDefault);
                    passFailByRun.AppendLine($"{run.Key.Performance.MedianCpuUsed:N0}%", _colorScheme.Default);

                    passFailByRun.Append($"  Peak Memory: ", _colorScheme.DarkDefault);
                    passFailByRun.Append($"{DisplayUtil.GetFriendlyBytes((long)run.Key.Performance.PeakMemoryUsed)}", _colorScheme.Default);
                    passFailByRun.Append($", Median: ", _colorScheme.DarkDefault);
                    passFailByRun.AppendLine($"{DisplayUtil.GetFriendlyBytes((long)run.Key.Performance.MedianMemoryUsed)}", _colorScheme.Default);

                    passFailByRun.Append($"  Peak Disk Time: ", _colorScheme.DarkDefault);
                    passFailByRun.Append($"{run.Key.Performance.PeakDiskTime:N0}%", _colorScheme.Default);
                    passFailByRun.Append($", Median: ", _colorScheme.DarkDefault);
                    passFailByRun.AppendLine($"{run.Key.Performance.MedianDiskTime:N0}%", _colorScheme.Default);

                    passFailByRun.Append($"  Run Id: ", _colorScheme.Default);
                    passFailByRun.AppendLine(run.Key.CommanderRunId.ToString(), _colorScheme.DarkDefault);

                    passFailByRun.AppendLine(Environment.NewLine);
                }
            }

            // ***********************
            // Slowest Test Summary
            // ***********************
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
                    performance.Append($" \u2022 ");
                    performance.Append(DisplayUtil.GetPrettyTestName(test.FirstOrDefault().FullName));
                    performance.AppendLine($" {test.FirstOrDefault().Duration.ToElapsedTime()}", _colorScheme.Duration);
                }
                performance.AppendLine(Environment.NewLine);
            }

            // ***********************
            // Failed Tests Output
            // ***********************
            var testOutput = new ColorTextBuilder();
            var showErrors = _configuration.GenerateReportType.HasFlag(GenerateReportType.Errors);
            var showStackTraces = _configuration.GenerateReportType.HasFlag(GenerateReportType.StackTraces);
            var showTestOutput = _configuration.GenerateReportType.HasFlag(GenerateReportType.TestOutput);
            var showTestAnalysis = _configuration.GenerateReportType.HasFlag(GenerateReportType.TestAnalysis);
            if (showErrors || showStackTraces || showTestOutput)
            {
                if (!isPassed)
                {
                    WriteHeader(testOutput, "FAILED TESTS", _colorScheme.Error);
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
                        testOutput.Append(testIndexStr, _colorScheme.DarkError);
                        testOutput.AppendLine($"{test.TestName}", _colorScheme.Error);
                        testOutput.AppendLine($"{new string(' ', testIndexStr.Length)}{test.FullName.Replace($".{test.TestName}", "")}", _colorScheme.DarkDefault);
                        testOutput.AppendLine($"{new string(' ', testIndexStr.Length)}{test.RuntimeVersion}", _colorScheme.DarkDuration);
                        testOutput.AppendLine();

                        testOutput.Append($"  Duration: ", _colorScheme.DarkDefault);
                        testOutput.AppendLine($"{test.Duration.ToElapsedTime()}", _colorScheme.Duration);

                        if (showErrors && !string.IsNullOrEmpty(test.ErrorMessage))
                        {
                            testOutput.AppendLine($"  Error Output: ", _colorScheme.Bright);
                            testOutput.AppendLine(_lineSeparator, _colorScheme.DarkDefault);
                            testOutput.AppendLine($"{test.ErrorMessage}", _colorScheme.DarkError);
                            testOutput.AppendLine(_lineSeparator, _colorScheme.DarkDefault);
                        }
                        if (showStackTraces && !string.IsNullOrEmpty(test.StackTrace))
                        {
                            testOutput.AppendLine($"  Stack Trace:", _colorScheme.Bright);
                            testOutput.AppendLine(_lineSeparator, _colorScheme.DarkDefault);
                            testOutput.AppendLine($"{test.StackTrace}", _colorScheme.DarkError);
                            testOutput.AppendLine(_lineSeparator, _colorScheme.DarkDefault);
                        }
                        if (showTestOutput && !string.IsNullOrEmpty(test.TestOutput))
                        {
                            testOutput.AppendLine($"  Test Output: ", _colorScheme.Bright);
                            testOutput.AppendLine(_lineSeparator, _colorScheme.DarkDefault);
                            testOutput.AppendLine($"{test.TestOutput}", _colorScheme.Default);
                            testOutput.AppendLine(_lineSeparator, _colorScheme.DarkDefault);
                        }
                        testOutput.AppendLine(Environment.NewLine);
                    }
                }
            }

            // ***********************
            // Total Run Overview
            // ***********************
            _console.WriteLine();
            _console.WriteLine(ColorTextBuilder.Create.Append($"╔{_headerLine}{_headerLine}", _colorScheme.Highlight).Append($"{_headerChar}", _colorScheme.DarkHighlight).Append($"{_headerChar}", _colorScheme.DarkHighlight2).AppendLine($"{_headerChar}", _colorScheme.DarkHighlight3)
                .AppendLine($"{_headerBorderChar}  NUnit.Commander Test Report", _colorScheme.Highlight));
            var testRunIds = allReports.GroupBy(x => x.TestRunId).Select(x => x.Key);
            if (testRunIds?.Any() == true)
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", _colorScheme.Highlight).AppendLine($"  Test Run Id(s): {string.Join(", ", testRunIds)}"));
            var frameworks = _runContext.Runs.SelectMany(x => x.Key.Frameworks).Distinct();
            if (frameworks.Any() == true)
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", _colorScheme.Highlight).AppendLine($"  Framework(s): {string.Join(", ", frameworks)}"));
            var frameworkRuntimes = _runContext.Runs.SelectMany(x => x.Key.FrameworkRuntimes).Distinct();
            if (frameworkRuntimes.Any() == true)
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", _colorScheme.Highlight).AppendLine($"  Framework Runtime(s): {string.Join(", ", frameworkRuntimes)}"));
            var startTime = _runContext.Runs.Select(x => x.Key.StartTime).OrderBy(x => x).FirstOrDefault();
            var endTime = _runContext.Runs.Select(x => x.Key.EndTime).OrderByDescending(x => x).FirstOrDefault();
            _console.Write(ColorTextBuilder.Create.Append($"{_headerBorderChar}", _colorScheme.Highlight).Append($"  Test Start: {startTime}"));
            _console.Write($"  Test End: {endTime}");
            _console.WriteLine($"  Total Duration: {endTime.Subtract(startTime)}");
            _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", _colorScheme.Highlight).AppendLine($"  Settings:"));
            _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", _colorScheme.Highlight).AppendLine($"    Runtime={totalDuration}"));
            if (_console.IsOutputRedirected)
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", _colorScheme.Highlight).AppendLine($"    LogMode=Enabled"));
            else
                _console.WriteLine(ColorTextBuilder.Create.Append($"{_headerBorderChar}", _colorScheme.Highlight).AppendLine($"    LogMode=Disabled"));
            _console.WriteLine(ColorTextBuilder.Create.Append($"╚{_headerLine}{_headerLine}", _colorScheme.Highlight).Append($"{_headerChar}", _colorScheme.DarkHighlight).Append($"{_headerChar}", _colorScheme.DarkHighlight2).AppendLine($"{_headerChar}", _colorScheme.DarkHighlight3));

            // ***********************
            // PASSED / FAILED ascii art
            // ***********************
            if (isPassed)
                _console.WriteAscii(ColorTextBuilder.Create.Append("PASSED", _colorScheme.Success));
            else
                _console.WriteAscii(ColorTextBuilder.Create.Append("FAILED", _colorScheme.Error));

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
                if(_runContext.HistoryReport != null)
                    _console.WriteLine(_runContext.HistoryReport.BuildReport());
            }
            if (passFailByRun.Length > 0)
                _console.WriteLine(passFailByRun);
            if (passFail.Length > 0)
                _console.WriteLine(passFail);
        }

        private void WriteHeader(ColorTextBuilder builder, string str, Color? color = null)
        {
            builder.AppendLine($"╔{new string(_headerChar, str.Length + 4)}╗", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{_headerBorderChar}  {str}  {_headerBorderChar}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"╚{new string(_headerChar, str.Length + 4)}╝", color ?? _colorScheme.Highlight);
        }
    }
}
