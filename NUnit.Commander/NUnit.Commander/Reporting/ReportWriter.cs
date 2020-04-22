using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Extensions;
using NUnit.Commander.Models;
using NUnit.Commander.Reporting.ReportWriters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace NUnit.Commander.Reporting
{
    /// <summary>
    /// Handles writing of reports
    /// </summary>
    public class ReportWriter
    {
        private const int DefaultBorderWidth = 50;
        private readonly ColorScheme _colorScheme;
        private readonly IExtendedConsole _console;
        private readonly ApplicationConfiguration _configuration;
        private readonly RunContext _runContext;
        private readonly string _headerLine;
        private readonly string _lineSeparator;
        private readonly bool _allowFileOperations;
        private readonly ReportFactory _reportFactory;

        public ReportWriter(IExtendedConsole console, ColorScheme colorScheme, ApplicationConfiguration configuration, RunContext runContext, bool allowFileOperations)
        {
            _console = console;
            _colorScheme = colorScheme;
            _configuration = configuration;
            _runContext = runContext;
            _reportFactory = new ReportFactory(_configuration, _console, _runContext, _colorScheme);
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
            var startTime = _runContext.Runs.Select(x => x.Key.StartTime).OrderBy(x => x).FirstOrDefault();
            var endTime = _runContext.Runs.Select(x => x.Key.EndTime).OrderByDescending(x => x).FirstOrDefault();
            var totalDuration = TimeSpan.FromTicks(allReports.Sum(x => x.Duration.Ticks));
            var isPassed = allReports
                .SelectMany(x => x.Report.TestReports)
                .Count(x => x.TestStatus == TestStatus.Fail) == 0;
            if (isPassed)
                overallTestStatus = TestStatus.Pass;

            // ***********************
            // Total Run Summary
            // ***********************
            var passFail = _reportFactory.Create<ReportSummary>(allReports);

            // ***********************
            // Multiple Run Summary
            // ***********************
            var passFailByRun = _reportFactory.Create<ReportRunsSummary>(allReports);

            // ***********************
            // Slowest Test Summary
            // ***********************
            var performance = new ColorTextBuilder();
            if (_configuration.GenerateReportType.HasFlag(GenerateReportType.Performance))
                performance.Append(_reportFactory.Create<SlowestTestSummary>());

            // ***********************
            // Slowest Assemblies Summary
            // ***********************
            if (_configuration.GenerateReportType.HasFlag(GenerateReportType.Performance))
                performance.Append(_reportFactory.Create<SlowestTestSummary>());

            // ***********************
            // Charts
            // ***********************
            if (_configuration.GenerateReportType.HasFlag(GenerateReportType.Charts))
                performance.Append(_reportFactory.Create<StackedCharts>());

            // ***********************
            // Failed Tests Output
            // ***********************
            var testOutput = _reportFactory.Create<FailedTestsReport>(allReports);

            // ***********************
            // Total Run Overview
            // ***********************
            var overview = _reportFactory.Create<Overview>(allReports);

            // ***********************
            // Historical analysis
            // ***********************
            var historicalAnalysis = _reportFactory.Create<Overview>(allReports);


            // ***********************
            // Print all reports to console
            // ***********************
            _console.WriteLine(overview);

            if (isPassed)
                _console.WriteAscii(ColorTextBuilder.Create.Append("PASSED", _colorScheme.Success));
            else
                _console.WriteAscii(ColorTextBuilder.Create.Append("FAILED", _colorScheme.Error));

            if (performance.Length > 0)
                _console.WriteLine(performance);
            if (testOutput.Length > 0)
                _console.WriteLine(testOutput);
            if (historicalAnalysis.Length > 0)
                _console.WriteLine(historicalAnalysis);
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
                    builder.AppendLine(historicalAnalysis.ToString());
                    builder.AppendLine(passFailByRun);
                    builder.AppendLine(passFail);
                    var reportFilename = Path.GetFullPath(Path.Combine(_configuration.LogPath, $"{uniqueRunIds.FirstOrDefault()}-report.log"));
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

                        var reportFilename = Path.GetFullPath(Path.Combine(_configuration.LogPath, $"{run.Key.CommanderRunId}-tests.log"));
                        File.WriteAllText(reportFilename, builder.ToString());
                        _console.Write(ColorTextBuilder.Create.AppendLine($"Wrote tests report to {reportFilename}", _colorScheme.DarkDefault));
                    }
                }
            }

            return overallTestStatus;
        }

        

        private void WriteSquareBox(ColorTextBuilder builder, string str, int leftPadding = 0, Color? color = null)
        {
            builder.AppendLine($"{UTF8Constants.BoxTopLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.BoxHorizontal)}{UTF8Constants.BoxTopRight}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.BoxVertical}  {str}  {UTF8Constants.BoxVertical}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.BoxBottomLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.BoxHorizontal)}{UTF8Constants.BoxBottomRight}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
        }

        private void WriteRoundBox(ColorTextBuilder builder, string str, int leftPadding = 0, Color? color = null)
        {
            builder.AppendLine($"{UTF8Constants.RoundBoxTopLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.RoundBoxHorizontal)}{UTF8Constants.RoundBoxTopRight}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.RoundBoxVertical}  {str}  {UTF8Constants.RoundBoxVertical}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
            builder.AppendLine($"{UTF8Constants.RoundBoxBottomLeft}{DisplayUtil.Pad(str.Length + 4, UTF8Constants.RoundBoxHorizontal)}{UTF8Constants.RoundBoxBottomRight}{DisplayUtil.Pad(leftPadding)}", color ?? _colorScheme.Highlight);
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
