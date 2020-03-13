using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Extensions;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NUnit.Commander.Analysis
{
    public class TestHistoryAnalyzer
    {
        TestHistoryDatabaseProvider _testHistoryDatabaseProvider;
        ApplicationConfiguration _configuration;

        public TestHistoryAnalyzer(ApplicationConfiguration configuration, TestHistoryDatabaseProvider testHistoryDatabaseProvider)
        {
            _testHistoryDatabaseProvider = testHistoryDatabaseProvider;
            _configuration = configuration;
        }

        public HistoryReport Analyze(IEnumerable<TestHistoryEntry> currentRun)
        {
            var report = new HistoryReport(new ColorManager(_configuration.ColorScheme));

            var data = _testHistoryDatabaseProvider.Database.Entries;
            var testsByName = data.GroupBy(x => x.FullName);
            var dataCopy = data.ToList();
            dataCopy.AddRange(currentRun);
            var testsByNameWithCurrent = dataCopy.GroupBy(x => x.FullName);
            var total = 0;
            var passed = 0;
            var failed = 0;
            report.TotalDataPoints = data.GroupBy(x => x.CommanderRunId).Count();

            // look for unstable tests. Those are defined as tests which have a high ratio of pass/fail
            var ratio = string.Empty;
            var percentage = 0.0;
            foreach (var testgroup in testsByNameWithCurrent)
            {
                // CommanderRunId gives us a better indication of individual runs, rather than by test name
                total = testgroup.GroupBy(x => x.CommanderRunId).Count();
                // if there isn't enough data, skip the check
                if (total < _configuration.HistoryAnalysisConfiguration.MinTestHistoryToAnalyze)
                    continue;
                var test = testgroup.Key;
                var tests = testgroup.ToList();
                var gcd = (double)MathExtensions.GCD(passed, failed);
                passed = tests.Count(x => x.IsPass);
                failed = tests.Count() - passed;
                // express ratio as a percentage
                if (gcd > 0)
                    ratio = $"{(failed / gcd)}:{(passed / gcd)}";
                else
                    ratio = $"{failed}:{passed}";
                percentage = (double)failed / tests.Count();
                if (percentage > _configuration.HistoryAnalysisConfiguration.MinTestReliabilityThreshold)
                {
                    // only report test if it's contained in this run. That way deleted and renamed tests will be filtered out.
                    if (currentRun.Any(x => x.FullName == test))
                        report.UnstableTests.Add(new TestPoint(test, passed, failed, tests.Count(), percentage, ratio));
                }
            }

            // look for duration changes
            foreach (var testgroup in testsByName)
            {
                // CommanderRunId gives us a better indication of individual runs, rather than by test name
                total = testgroup.GroupBy(x => x.CommanderRunId).Count();
                // if there isn't enough data, skip the check
                if (total < _configuration.HistoryAnalysisConfiguration.MinTestHistoryToAnalyze)
                    continue;
                var test = testgroup.Key;
                var tests = testgroup.ToList();
                var medianDuration = (long)tests.Median(x => x.Duration.Ticks);
                // var avgDuration = (long)tests.Average(x => x.Duration.Ticks);
                var threshold = _configuration.HistoryAnalysisConfiguration.MaxTestDurationChange;
                var differenceThreshold = medianDuration * threshold;
                // look for tests with duration changes
                var currentRunChanges = currentRun
                    .Where(x => x.FullName == test 
                        && x.Duration.TotalMilliseconds >= _configuration.HistoryAnalysisConfiguration.MinTestMillisecondsForDurationAnalysis
                        && Math.Abs(x.Duration.Ticks - medianDuration) >= differenceThreshold)
                    .GroupBy(x => x.FullName)
                    .ToList();
                if (currentRunChanges.Any())
                {
                    var currentEntry = currentRun
                        .Where(x => x.FullName == test)
                        .OrderByDescending(x => x.Duration)
                        .First();
                    var durationChange = TimeSpan.FromTicks(currentEntry.Duration.Ticks - medianDuration);
                    var anomaly = new TestPoint(test, durationChange, currentEntry.Duration, TimeSpan.FromTicks(medianDuration));
                    // only report test if it's contained in this run. That way deleted and renamed tests will be filtered out.
                    if (currentRun.Any(x => x.FullName == test) && durationChange.TotalMilliseconds >= _configuration.HistoryAnalysisConfiguration.MinTestMillisecondsForDurationAnalysis)
                        report.DurationAnomalyTests.Add(anomaly);
                }
            }

            return report;
        }
    }
}
