using AnyConsole;
using NUnit.Commander.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace NUnit.Commander.Models
{
    public class HistoryReport
    {
        public List<TestPoint> UnstableTests { get; set; } = new List<TestPoint>();
        public List<TestPoint> DurationAnomalyTests { get; set; } = new List<TestPoint>();

        /// <summary>
        /// The total number of data points used to compute the report
        /// </summary>
        public int TotalDataPoints { get; set; }

        public ColorTextBuilder BuildReport()
        {
            var str = new ColorTextBuilder();

            str.Append("  Total Runs Analyzed: [", Color.White).Append($"{TotalDataPoints}", Color.Yellow).AppendLine("]", Color.White);
            str.AppendLine();
            str.Append("  Unstable Tests:", Color.White);
            if (UnstableTests.Count == 0)
                str.AppendLine(" <none>");
            else
            {
                str.AppendLine();
                foreach (var test in UnstableTests)
                {
                    var lastDot = test.FullName.LastIndexOf(".");
                    var testName = test.FullName.Substring(lastDot + 1, test.FullName.Length - lastDot - 1);
                    str.Append($" \u2022 {test.FullName.Replace(testName, "")}")
                        .Append($"{testName} ", Color.White)
                        .Append("[", Color.White)
                        .Append(string.Format("{0:0.0}% failures", test.Percentage * 100.0), Color.Red)
                        .Append(", ")
                        .Append(test.Ratio, Color.Red)
                        .AppendLine("]", Color.White);
                }
            }

            str.AppendLine();
            str.Append("  Duration Anomalies:", Color.White);
            if (DurationAnomalyTests.Count == 0)
                str.AppendLine(" <none>");
            else
            {
                str.AppendLine();
                foreach (var test in DurationAnomalyTests)
                {
                    // test got slower
                    var color = Color.Red;
                    var direction = "+";
                    if (test.DurationChange.Ticks < 0)
                    {
                        // test got faster
                        color = Color.Green;
                        direction = "";
                    }
                    var lastDot = test.FullName.LastIndexOf(".");
                    var testName = test.FullName.Substring(lastDot + 1, test.FullName.Length - lastDot - 1);
                    str.Append($" \u2022 {test.FullName.Replace(testName, "")}")
                        .Append($"{testName} ", Color.White)
                        .Append($"Diff", Color.White)
                        .Append("[", Color.White)
                        .Append($"{direction}{test.DurationChange.ToElapsedTime()}", color)
                        .Append("]", Color.White)
                        .Append($" Time", Color.White)
                        .Append("[", Color.White)
                        .Append($"{test.Duration.ToElapsedTime()}", Color.Cyan)
                        .Append("]", Color.White)
                        .Append($" Median", Color.White)
                        .Append("[", Color.White)
                        .Append($"{test.Average.ToElapsedTime()}", Color.Cyan)
                        .AppendLine("]", Color.White);
                }
            }
            str.AppendLine();

            return str;
        }
    }

    public struct TestPoint
    {
        public string FullName { get; set; }
        public int Pass { get; set; }
        public int Fail { get; set; }
        public int Total { get; set; }
        public double Percentage { get; set; }
        public string Ratio { get; set; }
        public TimeSpan DurationChange { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan Average { get; set; }
        public TestPoint(string fullName, int pass, int fail, int total, double percentage, string ratio)
        {
            FullName = fullName;
            Pass = pass;
            Fail = fail;
            Total = total;
            Percentage = percentage;
            Ratio = ratio;
            DurationChange = TimeSpan.Zero;
            Duration = TimeSpan.Zero;
            Average = TimeSpan.Zero;
        }
        public TestPoint(string fullName, TimeSpan durationChange, TimeSpan duration, TimeSpan average)
        {
            FullName = fullName;
            Pass = 0;
            Fail = 0;
            Total = 0;
            Percentage = 0;
            Ratio = "";
            DurationChange = durationChange;
            Duration = duration;
            Average = average;
        }
    }
}
