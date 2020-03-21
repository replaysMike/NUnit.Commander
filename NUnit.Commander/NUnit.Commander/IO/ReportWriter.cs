using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Extensions;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace NUnit.Commander.IO
{
    /// <summary>
    /// Handles writing of reports
    /// </summary>
    public class ReportWriter
    {
        private const int DefaultBorderWidth = 50;
        private readonly ColorManager _colorScheme;
        private readonly IExtendedConsole _console;
        private readonly ApplicationConfiguration _configuration;
        private readonly RunContext _runContext;
        private readonly string _headerLine;
        private readonly string _lineSeparator;
        private readonly bool _allowFileOperations;

        public ReportWriter(IExtendedConsole console, ColorManager colorScheme, ApplicationConfiguration configuration, RunContext runContext, bool allowFileOperations)
        {
            _console = console;
            _colorScheme = colorScheme;
            _configuration = configuration;
            _runContext = runContext;
            _allowFileOperations = allowFileOperations;
            // generate the header/lines art by width
            _headerLine = new string(UTF8Constants.BoxHorizontal, DefaultBorderWidth);
            _lineSeparator = new string(UTF8Constants.HorizontalLine, !_console.IsOutputRedirected ? (Console.WindowWidth - 1) : DefaultBorderWidth);
        }

        /// <summary>
        /// Write the final report to the console output
        /// </summary>
        /// <param name="allReports"></param>
        /// <param name="eventLog"></param>
        public TestStatus WriteFinalReport()
        {
            var overallTestStatus = TestStatus.Fail;

            var uniqueRunIds = _runContext.Runs.Select(x => x.Key.CommanderRunId).Distinct();
            var commanderIdMap = new Dictionary<Guid, ICollection<Guid>>();
            foreach (var commanderRunId in uniqueRunIds)
                commanderIdMap.Add(commanderRunId, _runContext.Runs.SelectMany(x => x.Value.Select(y => y.TestRunId)).Distinct().ToList());
            var allReports = _runContext.Runs.SelectMany(x => x.Value);

            // ***********************
            // Total Run Summary
            // ***********************
            var passFail = new ColorTextBuilder();
            var totalDuration = TimeSpan.FromTicks(allReports.Sum(x => x.Duration.Ticks));
            var isPassed = allReports
                .SelectMany(x => x.Report.TestReports)
                .Count(x => x.TestStatus == TestStatus.Fail) == 0;
            if (isPassed)
                overallTestStatus = TestStatus.Pass;
            if (_configuration.GenerateReportType.HasFlag(GenerateReportType.PassFail))
            {
                var statusColor = _colorScheme.Error;
                var successColor = _colorScheme.Default;
                var failuresColor = _colorScheme.Default;
                var errorsColor = _colorScheme.Default;
                var warningsColor = _colorScheme.Default;

                var allSuccess = allReports.Sum(x => x.Failed) == 0 && allReports.Sum(x => x.Passed) > 0;
                var anyFailure = allReports.Sum(x => x.Failed) > 0;
                var testCount = allReports.Sum(x => x.TestCount);
                var passed = allReports.Sum(x => x.Passed);
                var failed = allReports.Sum(x => x.Failed);
                var warnings = allReports.Sum(x => x.Warnings);
                var asserts = allReports.Sum(x => x.Asserts);
                var inconclusive = allReports.Sum(x => x.Inconclusive);
                var errors = allReports.SelectMany(x => x.Report.TestReports.Where(t => t.TestStatus == TestStatus.Fail && !string.IsNullOrEmpty(t.ErrorMessage))).Count();
                var skipped = allReports.Sum(x => x.Skipped);
                var totalRuns = allReports.GroupBy(x => x.RunNumber).Count();

                if (allSuccess)
                {
                    successColor = _colorScheme.Success;
                    statusColor = _colorScheme.Success;
                }
                if (allReports.Sum(x => x.Failed) > 0)
                    failuresColor = _colorScheme.Error;
                if (errors > 0)
                    errorsColor = _colorScheme.DarkError;
                if (warnings > 0)
                    warningsColor = _colorScheme.DarkHighlight;

                WriteSquareBox(passFail, "Test Run Summary");

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
                passFail.Append($"{errors:N0}", errorsColor);
                passFail.Append($", Warnings: ", _colorScheme.DarkDefault);
                passFail.Append($"{warnings:N0}", warningsColor);
                passFail.Append($", Ignored: ", _colorScheme.DarkDefault);
                passFail.AppendLine($"{skipped}", _colorScheme.Default);

                passFail.Append($"  Asserts: ", _colorScheme.DarkDefault);
                passFail.Append($"{asserts:N0}", _colorScheme.Default);
                passFail.Append($", Inconclusive: ", _colorScheme.DarkDefault);
                passFail.AppendLine($"{inconclusive:N0}", _colorScheme.Default);

                if (_runContext.Runs?.Any() == true)
                {
                    passFail.Append($"  Peak Cpu: ", _colorScheme.DarkDefault);
                    passFail.Append($"{_runContext.Runs.Max(x => x.Key.Performance.PeakCpuUsed):N0}%", _colorScheme.Default);
                    passFail.Append($", Median: ", _colorScheme.DarkDefault);
                    passFail.AppendLine($"{_runContext.Runs.Median(x => x.Key.Performance.MedianCpuUsed):N0}%", _colorScheme.Default);

                    passFail.Append($"  Peak Memory: ", _colorScheme.DarkDefault);
                    passFail.Append($"{DisplayUtil.GetFriendlyBytes((long)_runContext.Runs.Max(x => x.Key.Performance.PeakMemoryUsed))}", _colorScheme.Default);
                    passFail.Append($", Median: ", _colorScheme.DarkDefault);
                    passFail.AppendLine($"{DisplayUtil.GetFriendlyBytes((long)_runContext.Runs.Median(x => x.Key.Performance.MedianMemoryUsed))}", _colorScheme.Default);

                    passFail.Append($"  Peak Disk Time: ", _colorScheme.DarkDefault);
                    passFail.Append($"{_runContext.Runs.Max(x => x.Key.Performance.PeakDiskTime):N0}%", _colorScheme.Default);
                    passFail.Append($", Median: ", _colorScheme.DarkDefault);
                    passFail.AppendLine($"{_runContext.Runs.Median(x => x.Key.Performance.MedianDiskTime):N0}%", _colorScheme.Default);
                }

                passFail.AppendLine(Environment.NewLine);
            }

            // ***********************
            // Multiple Run Summary
            // ***********************
            var passFailByRun = new ColorTextBuilder();
            if (_runContext.Runs.Count > 1)
            {
                var runNumber = 0;
                foreach (var run in _runContext.Runs)
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

                    WriteRoundBox(passFailByRun, $"Test Run #{runNumber} Summary", _colorScheme.DarkHighlight);

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

                    passFailByRun.Append($"  Peak Test Concurrency: ", _colorScheme.DarkDefault);
                    passFailByRun.Append($"{run.Key.Performance.PeakTestConcurrency:N0}%", _colorScheme.Default);
                    passFailByRun.Append($", Median: ", _colorScheme.DarkDefault);
                    passFailByRun.AppendLine($"{run.Key.Performance.MedianTestConcurrency:N0}%", _colorScheme.Default);

                    passFailByRun.Append($"  Peak Test Fixture Concurrency: ", _colorScheme.DarkDefault);
                    passFailByRun.Append($"{run.Key.Performance.PeakTestFixtureConcurrency:N0}%", _colorScheme.Default);
                    passFailByRun.Append($", Median: ", _colorScheme.DarkDefault);
                    passFailByRun.AppendLine($"{run.Key.Performance.MedianTestFixtureConcurrency:N0}%", _colorScheme.Default);

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
                WriteRoundBox(performance, $"Top {_configuration.SlowestTestsCount} slowest tests");
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
                    performance.Append($" {UTF8Constants.Bullet} ");
                    performance.Append(DisplayUtil.GetPrettyTestName(test.FirstOrDefault().FullName, _colorScheme.DarkDefault, _colorScheme.Default, _colorScheme.DarkDefault));
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
                    WriteRoundBox(testOutput, "FAILED TESTS", _colorScheme.Error);
                }

                var testIndex = 0;
                var failedTestCases = allReports
                    .GroupBy(x => x.RunNumber)
                    .SelectMany(x => x.ToDictionary(y => y.RunNumber, value => value.Report.TestReports.Where(x => x.TestStatus == TestStatus.Fail)));
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

                        testOutput.Append(testIndexStr, _colorScheme.DarkError, _colorScheme.RaisedBackground);
                        testOutput.Append(test.TestName, _colorScheme.Error, _colorScheme.RaisedBackground);
                        if (!_console.IsOutputRedirected)
                            testOutput.AppendLine($"{DisplayUtil.Pad(Console.WindowWidth - testIndexStr.Length - test.TestName.Length)}", _colorScheme.Error, _colorScheme.RaisedBackground);
                        else
                            testOutput.AppendLine();
                        var fullName = $"{DisplayUtil.Pad(testIndexStr.Length)}{test.FullName.Replace($".{test.TestName}", "")}";
                        testOutput.Append(fullName, _colorScheme.DarkDuration, _colorScheme.RaisedBackground);
                        if (!_console.IsOutputRedirected)
                            testOutput.AppendLine($"{DisplayUtil.Pad(Console.WindowWidth - fullName.Length)}", _colorScheme.Error, _colorScheme.RaisedBackground);
                        else
                            testOutput.AppendLine();

                        var runtimeVersion = $"{DisplayUtil.Pad(testIndexStr.Length)}{test.RuntimeVersion}";
                        testOutput.Append($"{runtimeVersion}", _colorScheme.DarkDefault, _colorScheme.Background ?? Color.Black);
                        if (!_console.IsOutputRedirected)
                            testOutput.AppendLine($"{DisplayUtil.Pad(Console.WindowWidth - runtimeVersion.Length)}", _colorScheme.Error, _colorScheme.Background ?? Color.Black);
                        else
                            testOutput.AppendLine();

                        testOutput.AppendLine();

                        testOutput.Append($"  Duration ", _colorScheme.DarkDefault);
                        testOutput.AppendLine($"{test.Duration.ToElapsedTime()}", _colorScheme.Duration);

                        if (showErrors && !string.IsNullOrEmpty(test.ErrorMessage))
                        {
                            testOutput.AppendLine($"  Error Output ", _colorScheme.Bright);
                            testOutput.AppendLine(_lineSeparator, lineSeparatorColor);
                            testOutput.AppendLine($"{test.ErrorMessage}", _colorScheme.DarkDefault);
                            testOutput.AppendLine(_lineSeparator, lineSeparatorColor);
                        }
                        if (showStackTraces && !string.IsNullOrEmpty(test.StackTrace))
                        {
                            testOutput.AppendLine($"  Stack Trace:", _colorScheme.Bright);
                            testOutput.AppendLine(_lineSeparator, lineSeparatorColor);
                            testOutput.Append(StackTracePrettify.Format(test.StackTrace, _colorScheme));
                            testOutput.AppendLine();
                            testOutput.AppendLine(_lineSeparator, lineSeparatorColor);
                        }
                        if (showTestOutput && !string.IsNullOrEmpty(test.TestOutput))
                        {
                            testOutput.AppendLine($"  Test Output: ", _colorScheme.Bright);
                            testOutput.AppendLine(_lineSeparator, lineSeparatorColor);
                            testOutput.AppendLine($"{test.TestOutput}", _colorScheme.Default);
                            testOutput.AppendLine(_lineSeparator, lineSeparatorColor);
                        }
                        testOutput.AppendLine(Environment.NewLine);
                    }
                }
            }

            // ***********************
            // Total Run Overview
            // ***********************
            var overview = new ColorTextBuilder();
            overview.AppendLine();
            overview.Append(ColorTextBuilder.Create.Append($"╔{_headerLine}{_headerLine}", _colorScheme.Highlight).Append($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight).Append($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight2).AppendLine($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight3)
                .AppendLine($"{UTF8Constants.BoxVertical}  NUnit.Commander Test Report", _colorScheme.Highlight));
            var testRunIds = allReports.GroupBy(x => x.TestRunId).Select(x => x.Key);
            if (testRunIds?.Any() == true)
                overview.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"  Test Run Id(s): {string.Join(", ", testRunIds)}"));
            var frameworks = _runContext.Runs.SelectMany(x => x.Key.Frameworks).Distinct();
            if (frameworks.Any() == true)
                overview.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"  Framework(s): {string.Join(", ", frameworks)}"));
            var frameworkRuntimes = _runContext.Runs.SelectMany(x => x.Key.FrameworkRuntimes).Distinct();
            if (frameworkRuntimes.Any() == true)
                overview.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"  Framework Runtime(s): {string.Join(", ", frameworkRuntimes)}"));
            var startTime = _runContext.Runs.Select(x => x.Key.StartTime).OrderBy(x => x).FirstOrDefault();
            var endTime = _runContext.Runs.Select(x => x.Key.EndTime).OrderByDescending(x => x).FirstOrDefault();
            overview.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).Append($"  Test Start: {startTime}"));
            overview.Append($"  Test End: {endTime}");
            overview.AppendLine($"  Total Duration: {endTime.Subtract(startTime)}");
            overview.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"  Settings:"));
            overview.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"    Runtime={totalDuration}"));
            if (_console.IsOutputRedirected)
                overview.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"    LogMode=Enabled"));
            else
                overview.Append(ColorTextBuilder.Create.Append($"{UTF8Constants.BoxVertical}", _colorScheme.Highlight).AppendLine($"    LogMode=Disabled"));
            overview.Append(ColorTextBuilder.Create.Append($"╚{_headerLine}{_headerLine}", _colorScheme.Highlight).Append($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight).Append($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight2).AppendLine($"{UTF8Constants.BoxHorizontal}", _colorScheme.DarkHighlight3));

            _console.WriteLine(overview);

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
            var testAnalysisOutput = new ColorTextBuilder();
            if (showTestAnalysis)
            {
                WriteRoundBox(testAnalysisOutput, "Historical Analysis Report");
                _console.WriteLine(testAnalysisOutput);
                // write the analysis report
                if (_runContext.HistoryReport != null)
                    _console.WriteLine(_runContext.HistoryReport.BuildReport());
            }
            if (passFailByRun.Length > 0)
                _console.WriteLine(passFailByRun);
            if (passFail.Length > 0)
                _console.WriteLine(passFail);

            if (_allowFileOperations)
            {
                EnsurePathIsCreated(_configuration.LogPath);

                if (_configuration.EnableReportLog)
                {
                    var builder = new StringBuilder();
                    builder.AppendLine(overview.ToString());
                    builder.AppendLine(performance.ToString());
                    builder.AppendLine(testOutput.ToString());
                    builder.AppendLine(testAnalysisOutput.ToString());
                    builder.AppendLine(passFailByRun);
                    builder.AppendLine(passFail);
                    var reportFilename = Path.Combine(_configuration.LogPath, $"{uniqueRunIds.FirstOrDefault()}-report.log");
                    File.WriteAllText(reportFilename, builder.ToString());
                    _console.Write(ColorTextBuilder.Create.AppendLine($"Wrote summary report to {reportFilename}", _colorScheme.DarkDefault));
                }

                if (_configuration.EnableTestLog)
                {
                    // write out test logs for each run
                    foreach (var run in _runContext.Runs)
                    {
                        var builder = new StringBuilder();
                        builder.AppendLine($"FullName,Duration,TestStatus,StartTime,EndTime,RuntimeVersion");
                        foreach (var test in run.Value.SelectMany(x => x.Report.TestReports).Where(x => x.TestStatus != TestStatus.Skipped).OrderBy(x => x.StartTime))
                        {
                            // encode quotes in the test name with double quotes
                            var testName = test.FullName.Replace("\"", "\"\"");
                            builder.AppendLine($"\"{testName}\",\"{test.Duration.TotalMilliseconds:N2}ms\",\"{test.TestStatus}\",\"{test.StartTime.ToString(Constants.TimeFormat)}\",\"{test.EndTime.ToString(Constants.TimeFormat)}\",\"{test.RuntimeVersion}\"");
                        }

                        var reportFilename = Path.Combine(_configuration.LogPath, $"{run.Key.CommanderRunId}-tests.log");
                        File.WriteAllText(reportFilename, builder.ToString());
                        _console.Write(ColorTextBuilder.Create.AppendLine($"Wrote tests report to {reportFilename}", _colorScheme.DarkDefault));
                    }
                }
            }

            return overallTestStatus;
        }

        private void WriteSquareBox(ColorTextBuilder builder, string str, Color? color = null)
        {
            builder.AppendLine($"{UTF8Constants.BoxTopLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.BoxHorizontal)}{UTF8Constants.BoxTopRight}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.BoxVertical}  {str}  {UTF8Constants.BoxVertical}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.BoxBottomLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.BoxHorizontal)}{UTF8Constants.BoxBottomRight}", color ?? _colorScheme.Highlight);
        }

        private void WriteRoundBox(ColorTextBuilder builder, string str, Color? color = null)
        {
            builder.AppendLine($"{UTF8Constants.RoundBoxTopLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.RoundBoxHorizontal)}{UTF8Constants.RoundBoxTopRight}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.RoundBoxVertical}  {str}  {UTF8Constants.RoundBoxVertical}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.RoundBoxBottomLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.RoundBoxHorizontal)}{UTF8Constants.RoundBoxBottomRight}", color ?? _colorScheme.Highlight);
        }

        private bool EnsurePathIsCreated(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception ex)
            {
                _console.WriteLine($"Error: Could not create logging path at {path}. {ex.GetBaseException().Message}");
            }
            return false;
        }
    }
}
