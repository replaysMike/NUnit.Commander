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
    public class ReportRunsSummary : ReportBase
    {
        public ReportRunsSummary(ApplicationConfiguration configuration, IExtendedConsole console, RunContext runContext, ColorScheme colorScheme) : base(configuration, console, runContext, colorScheme) { }

        public override ColorTextBuilder Write(object parameters = null)
        {
            var allReports = (IEnumerable<DataEvent>)parameters;
            var builder = new ColorTextBuilder();
            var totalDuration = TimeSpan.FromTicks(allReports.Sum(x => x.Duration.Ticks));
            var isPassed = allReports.Sum(x => x.Failed) == 0;
            if (_runContext.Runs.Count > 1)
            {
                var runNumber = 0;
                foreach (var run in _runContext.Runs)
                {
                    runNumber++;

                    var allSuccess = run.Value.Sum(x => x.Failed) == 0;
                    var anyFailure = run.Value.Sum(x => x.Failed) > 0;
                    var statusColor = _colorScheme.Error;
                    var successColor = _colorScheme.Default;
                    var failuresColor = _colorScheme.Default;
                    var errorsColor = _colorScheme.Default;
                    var warningsColor = _colorScheme.Default;

                    var testRunner = run.Value.Select(x => x.TestRunner).FirstOrDefault();
                    var testRuntime = run.Value.Select(x => x.Runtime).FirstOrDefault();
                    var testCount = run.Value.Sum(x => x.TestCount);
                    var passed = run.Value.Sum(x => x.Passed);
                    var failed = run.Value.Sum(x => x.Failed);
                    var warnings = run.Value.Sum(x => x.Warnings);
                    var asserts = run.Value.Sum(x => x.Asserts);
                    var inconclusive = run.Value.Sum(x => x.Inconclusive);
                    var errors = run.Value.SelectMany(x => x.Report.TestReports.Where(t => t.TestStatus == TestStatus.Fail && !string.IsNullOrEmpty(t.ErrorMessage))).Count();
                    var skipped = run.Value.Sum(x => x.Skipped);
                    var totalRuns = run.Value.GroupBy(x => x.RunNumber).Count();

                    if (allSuccess)
                    {
                        successColor = _colorScheme.Success;
                        statusColor = _colorScheme.Success;
                    }
                    if (failed > 0)
                        failuresColor = _colorScheme.Error;
                    if (errors > 0)
                        errorsColor = _colorScheme.DarkError;
                    if (warnings > 0)
                        warningsColor = _colorScheme.DarkHighlight;

                    WriteRoundBox(builder, $"Test Run #{runNumber} Summary", 0, _colorScheme.DarkHighlight);

                    builder.Append($"  Result: ", _colorScheme.Default);
                    builder.AppendLine(allSuccess ? "Passed" : "Failed", statusColor);

                    builder.Append($"  Duration: ", _colorScheme.Default);
                    builder.Append($"{testCount:N0} ", _colorScheme.Bright);
                    builder.Append($"tests run in ", _colorScheme.Default);
                    builder.AppendLine($"{run.Key.EndTime.Subtract(run.Key.StartTime).ToTotalElapsedTime()}", _colorScheme.Duration);

                    builder.Append($"  Test Framework: ", _colorScheme.Default);
                    builder.AppendLine($"{testRuntime:N0} ", _colorScheme.Bright);
                    builder.Append($"  Test Runner: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{testRunner:N0} ", _colorScheme.Default);
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

                    builder.Append($"  Peak Cpu: ", _colorScheme.DarkDefault);
                    builder.Append($"{run.Key.Performance.PeakCpuUsed:N0}%", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{run.Key.Performance.MedianCpuUsed:N0}%", _colorScheme.Default);

                    builder.Append($"  Peak Memory: ", _colorScheme.DarkDefault);
                    builder.Append($"{DisplayUtil.GetFriendlyBytes((long)run.Key.Performance.PeakMemoryUsed)}", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{DisplayUtil.GetFriendlyBytes((long)run.Key.Performance.MedianMemoryUsed)}", _colorScheme.Default);

                    builder.Append($"  Peak Disk Time: ", _colorScheme.DarkDefault);
                    builder.Append($"{run.Key.Performance.PeakDiskTime:N0}%", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{run.Key.Performance.MedianDiskTime:N0}%", _colorScheme.Default);

                    builder.Append($"  Peak Test Concurrency: ", _colorScheme.DarkDefault);
                    builder.Append($"{run.Key.Performance.PeakTestConcurrency:N0}%", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{run.Key.Performance.MedianTestConcurrency:N0}%", _colorScheme.Default);

                    builder.Append($"  Peak Test Fixture Concurrency: ", _colorScheme.DarkDefault);
                    builder.Append($"{run.Key.Performance.PeakTestFixtureConcurrency:N0}%", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{run.Key.Performance.MedianTestFixtureConcurrency:N0}%", _colorScheme.Default);

                    builder.Append($"  Peak Assembly Concurrency: ", _colorScheme.DarkDefault);
                    builder.Append($"{run.Key.Performance.PeakAssemblyConcurrency:N0}%", _colorScheme.Default);
                    builder.Append($", Median: ", _colorScheme.DarkDefault);
                    builder.AppendLine($"{run.Key.Performance.MedianAssemblyConcurrency:N0}%", _colorScheme.Default);

                    builder.Append($"  Run Id: ", _colorScheme.Default);
                    builder.AppendLine(run.Key.CommanderRunId.ToString(), _colorScheme.DarkDefault);

                    builder.AppendLine(Environment.NewLine);
                }
            }
            return builder;
        }
    }
}
