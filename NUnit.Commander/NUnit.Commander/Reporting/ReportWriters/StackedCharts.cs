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
    public class StackedCharts : ReportBase
    {
        public StackedCharts(ApplicationConfiguration configuration, IExtendedConsole console, RunContext runContext, ColorScheme colorScheme) : base(configuration, console, runContext, colorScheme) { }

        public override ColorTextBuilder Write(object parameters = null)
        {
            var builder = new ColorTextBuilder();
            var chartWidth = 30;
            var chartHeight = 10;
            var chartSpacing = 3;
            var testConcurrency = new ColorTextBuilder();
            var testData = _runContext.Runs.SelectMany(x => x.Key.PerformanceLog.GetAll(PerformanceLog.PerformanceType.TestConcurrency));
            if (testData.Sum(x => x.Value) > 0)
            {
                WriteRoundBox(testConcurrency, $"Test Concurrency", 8);
                var testConcurrencyChart = new AsciiChart(chartWidth, chartHeight);
                var testChartData = testData.ToDictionary(key => key.TimeSlot, value => value.Value);
                testConcurrency.Append(testConcurrencyChart.GraphXY(testChartData, _colorScheme.DarkSuccess, _colorScheme.DarkDefault));
                testConcurrency.AppendLine();
            }

            var testFixtureConcurrency = new ColorTextBuilder();
            var testFixtureData = _runContext.Runs.SelectMany(x => x.Key.PerformanceLog.GetAll(PerformanceLog.PerformanceType.TestFixtureConcurrency));
            if (testFixtureData.Sum(x => x.Value) > 0)
            {
                WriteRoundBox(testFixtureConcurrency, "Test Fixture Concurrency");
                var testFixtureConcurrencyChart = new AsciiChart(chartWidth, chartHeight);
                var testFixtureChartData = testFixtureData.ToDictionary(key => key.TimeSlot, value => value.Value);
                testFixtureConcurrency.Append(testFixtureConcurrencyChart.GraphXY(testFixtureChartData, _colorScheme.DarkHighlight, _colorScheme.DarkDefault));
                testFixtureConcurrency.AppendLine();
            }

            var assemblyConcurrency = new ColorTextBuilder();
            var assemblyData = _runContext.Runs.SelectMany(x => x.Key.PerformanceLog.GetAll(PerformanceLog.PerformanceType.AssemblyConcurrency));
            if (assemblyData.Sum(x => x.Value) > 0)
            {
                WriteRoundBox(assemblyConcurrency, "Assembly Concurrency", 4);
                var assemblyConcurrencyChart = new AsciiChart(chartWidth, chartHeight);
                var assemblyChartData = assemblyData.ToDictionary(key => key.TimeSlot, value => value.Value);
                assemblyConcurrency.Append(assemblyConcurrencyChart.GraphXY(assemblyChartData, _colorScheme.DarkError, _colorScheme.DarkDefault));
                assemblyConcurrency.AppendLine();
            }

            var cpuUsage = new ColorTextBuilder();
            var cpuUsageData = _runContext.Runs.SelectMany(x => x.Key.PerformanceLog.GetAll(PerformanceLog.PerformanceType.CpuUsed));
            if (cpuUsageData.Sum(x => x.Value) > 0)
            {
                WriteRoundBox(cpuUsage, "CPU Usage");
                var cpuUsageChart = new AsciiChart(chartWidth, chartHeight);
                var cpuUsageChartData = cpuUsageData.ToDictionary(key => key.TimeSlot, value => value.Value);
                cpuUsage.Append(cpuUsageChart.GraphXY(cpuUsageChartData, _colorScheme.Default, _colorScheme.DarkDefault));
                cpuUsage.AppendLine();
            }

            // stack the graphs horizontally, until they won't fit on the console
            var chartRows = new List<ColorTextBuilder>();
            var isNewRow = false;
            var charts = testConcurrency;
            chartRows.Add(charts);
            chartRows[chartRows.Count - 1] = StackGraph(chartRows.Last(), testFixtureConcurrency, chartSpacing, out isNewRow);
            if (isNewRow)
                chartRows.Add(ColorTextBuilder.Create.Append(testFixtureConcurrency));
            chartRows[chartRows.Count - 1] = StackGraph(chartRows.Last(), assemblyConcurrency, chartSpacing, out isNewRow);
            if (isNewRow)
                chartRows.Add(ColorTextBuilder.Create.Append(assemblyConcurrency));
            chartRows[chartRows.Count - 1] = StackGraph(chartRows.Last(), cpuUsage, chartSpacing, out isNewRow);
            if (isNewRow)
                chartRows.Add(ColorTextBuilder.Create.Append(cpuUsage));

            foreach (var chartRow in chartRows)
                builder.AppendLine(chartRow);
            return builder;
        }

        private ColorTextBuilder StackGraph(ColorTextBuilder existingChart, ColorTextBuilder newChart, int chartSpacing, out bool isNewRow)
        {
            isNewRow = false;
            if (newChart.Length > 0)
            {
                if (!_console.IsOutputRedirected && _console.WindowWidth > existingChart.Width + newChart.Width + chartSpacing)
                {
                    existingChart = existingChart.Interlace(newChart, chartSpacing);
                }
                else
                {
                    isNewRow = true;
                }
            }
            return existingChart;
        }
    }
}
