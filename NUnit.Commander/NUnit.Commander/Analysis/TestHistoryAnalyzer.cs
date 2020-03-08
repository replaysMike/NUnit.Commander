using NUnit.Commander.Configuration;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Commander.Extensions;

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
            var report = new HistoryReport();

            var data = _testHistoryDatabaseProvider.Database.Entries;
            var testsByName = data.GroupBy(x => x.FullName);
            var dataCopy = data.ToList();
            dataCopy.AddRange(currentRun);
            var testsByNameWithCurrent = dataCopy.GroupBy(x => x.FullName);
            var total = 0;
            var passed = 0;
            var failed = 0;

            // look for unstable tests. Those are defined as tests which have a high ratio of pass/fail
            var ratio = 0.0;
            foreach (var testgroup in testsByNameWithCurrent)
            {
                // CommanderRunId gives us a better indication of individual runs, rather than by test name
                total = testgroup.GroupBy(x => x.CommanderRunId).Count();
                // if there isn't enough data, skip the check
                if (total < _configuration.HistoryAnalysisConfiguration.MinTestHistoryToAnalyze)
                    continue;
                var test = testgroup.Key;
                var tests = testgroup.ToList();
                passed = tests.Count(x => x.IsPass);
                failed = tests.Count() - passed;
                ratio = (double)failed / passed;
                if (ratio > _configuration.HistoryAnalysisConfiguration.MinTestReliabilityThreshold)
                {
                    report.UnstableTests.Add(new TestPoint(test, passed, failed, tests.Count(), 1.0 - ratio));
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
                        && Math.Abs(x.Duration.Ticks - medianDuration) >= differenceThreshold)
                    .GroupBy(x => x.FullName)
                    .ToList();
                if (currentRunChanges.Any())
                {
                    var currentEntry = currentRun
                        .OrderByDescending(x => x.Duration)
                        .First();
                    var durationChange = TimeSpan.FromTicks(currentEntry.Duration.Ticks - medianDuration);
                    var anomaly = new TestPoint(test, durationChange, currentEntry.Duration, TimeSpan.FromTicks(medianDuration));
                    report.DurationAnomalyTests.Add(anomaly);
                }
            }

            return report;
        }
    }
}
