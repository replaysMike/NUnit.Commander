using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Extensions;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NUnit.Commander.Reporting.ReportWriters
{
    public class ReportSummary : ReportBase
    {
        public ReportSummary(ApplicationConfiguration configuration, IExtendedConsole console, RunContext runContext, ColorManager colorScheme) : base(configuration, console, runContext, colorScheme) { }

        public override ColorTextBuilder Write(object parameters = null)
        {
            var allReports = (IEnumerable<DataEvent>)parameters;
            var builder = new ColorTextBuilder();
            var totalDuration = TimeSpan.FromTicks(allReports.Sum(x => x.Duration.Ticks));
            var isPassed = allReports
                .SelectMany(x => x.Report.TestReports)
                .Count(x => x.TestStatus == TestStatus.Fail) == 0;
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

                WriteSquareBox(builder, "Test Run Summary");

                builder.Append($"  Overall result: ", _colorScheme.Default);
                builder.AppendLine(isPassed ? "Passed" : "Failed", statusColor);

                builder.Append($"  Duration: ", _colorScheme.Default);
                builder.Append($"{testCount:N0} ", _colorScheme.Bright);
                builder.Append($"tests run in ", _colorScheme.Default);
                builder.AppendLine($"{totalDuration.ToTotalElapsedTime()}", _colorScheme.Duration);
                builder.AppendLine("");

                builder.Append($"  Test Runs: ", _colorScheme.Default);
                builder.AppendLine($"{totalRuns}", _colorScheme.Bright);

                builder.Append($"  Test Count: ", _colorScheme.Default);
                builder.Append($"{testCount:N0}", _colorScheme.Bright);

                builder.Append($", Passed: ", _colorScheme.Default);
                builder.Append($"{passed:N0}", successColor);
                builder.Append($", Failed: ", _colorScheme.Default);
                builder.AppendLine($"{failed:N0}", failuresColor);

                builder.Append($"  Errors: ", _colorScheme.DarkDefault);
                builder.Append($"{errors:N0}", errorsColor);
                builder.Append($", Warnings: ", _colorScheme.DarkDefault);
                builder.Append($"{warnings:N0}", warningsColor);
                builder.Append($", Ignored: ", _colorScheme.DarkDefault);
                builder.AppendLine($"{skipped}", _colorScheme.Default);

                builder.Append($"  Asserts: ", _colorScheme.DarkDefault);
                builder.Append($"{asserts:N0}", _colorScheme.Default);
                builder.Append($", Inconclusive: ", _colorScheme.DarkDefault);
                builder.AppendLine($"{inconclusive:N0}", _colorScheme.Default);

                if (_runContext.Runs?.Any() == true)
                {
                    builder.Append($"  Peak Cpu: ", _colorScheme.DarkDefault);
                    builder.Append($"{_runContext.Runs.Max(x => x.Key.Performance.PeakCpuUsed):N0}%", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{_runContext.Runs.Median(x => x.Key.Performance.MedianCpuUsed):N0}%", _colorScheme.Default);

                    builder.Append($"  Peak Memory: ", _colorScheme.DarkDefault);
                    builder.Append($"{DisplayUtil.GetFriendlyBytes((long)_runContext.Runs.Max(x => x.Key.Performance.PeakMemoryUsed))}", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{DisplayUtil.GetFriendlyBytes((long)_runContext.Runs.Median(x => x.Key.Performance.MedianMemoryUsed))}", _colorScheme.Default);

                    builder.Append($"  Peak Disk Time: ", _colorScheme.DarkDefault);
                    builder.Append($"{_runContext.Runs.Max(x => x.Key.Performance.PeakDiskTime):N0}%", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{_runContext.Runs.Median(x => x.Key.Performance.MedianDiskTime):N0}%", _colorScheme.Default);

                    builder.Append($"  Peak Test Concurrency: ", _colorScheme.DarkDefault);
                    builder.Append($"{_runContext.Runs.Max(x => x.Key.Performance.PeakTestConcurrency):N0}%", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{_runContext.Runs.Max(x => x.Key.Performance.MedianTestConcurrency):N0}%", _colorScheme.Default);

                    builder.Append($"  Peak Test Fixture Concurrency: ", _colorScheme.DarkDefault);
                    builder.Append($"{_runContext.Runs.Max(x => x.Key.Performance.PeakTestFixtureConcurrency):N0}%", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{_runContext.Runs.Max(x => x.Key.Performance.MedianTestFixtureConcurrency):N0}%", _colorScheme.Default);

                    builder.Append($"  Peak Assembly Concurrency: ", _colorScheme.DarkDefault);
                    builder.Append($"{_runContext.Runs.Max(x => x.Key.Performance.PeakAssemblyConcurrency):N0}%", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{_runContext.Runs.Max(x => x.Key.Performance.MedianAssemblyConcurrency):N0}%", _colorScheme.Default);
                }

                builder.AppendLine(Environment.NewLine);
            }
            return builder;
        }
    }
}
