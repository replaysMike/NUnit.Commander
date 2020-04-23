using AnyConsole;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.Extensions;
using System;
using System.Collections.Generic;

namespace NUnit.Commander.Models
{
    public class HistoryReport
    {
        private const string _bulletChar = "\u2022";
        private readonly ColorScheme _colorScheme;
        public List<TestPoint> UnstableTests { get; set; } = new List<TestPoint>();
        public List<TestPoint> DurationAnomalyTests { get; set; } = new List<TestPoint>();

        /// <summary>
        /// The total number of data points used to compute the report
        /// </summary>
        public int TotalDataPoints { get; set; }

        public HistoryReport(ColorScheme colorScheme)
        {
            _colorScheme = colorScheme;
        }

        public ColorTextBuilder BuildReport()
        {
            var str = new ColorTextBuilder();

            str.Append($"  Total Runs Analyzed: {UTF8Constants.LeftBracket}", _colorScheme.Bright).Append($"{TotalDataPoints}", _colorScheme.Highlight).AppendLine($"{UTF8Constants.RightBracket}", _colorScheme.Bright);
            str.AppendLine();
            str.Append("  Unstable Tests:", _colorScheme.Bright);
            if (UnstableTests.Count == 0)
                str.AppendLine(" <none>");
            else
            {
                str.AppendLine();
                foreach (var test in UnstableTests)
                {
                    str.Append($" {_bulletChar} ")
                        .Append(DisplayUtil.GetPrettyTestName(test.FullName, _colorScheme.DarkDefault, _colorScheme.Default, _colorScheme.DarkDefault))
                        .Append($"{UTF8Constants.LeftBracket}", _colorScheme.Bright)
                        .Append(string.Format("{0:0.0}% failures", test.Percentage * 100.0), _colorScheme.Error)
                        .Append(", ")
                        .Append(test.Ratio, _colorScheme.Error)
                        .AppendLine($"{UTF8Constants.RightBracket}", _colorScheme.Bright);
                }
            }

            str.AppendLine();
            str.Append("  Duration Anomalies:", _colorScheme.Bright);
            if (DurationAnomalyTests.Count == 0)
                str.AppendLine(" <none>");
            else
            {
                str.AppendLine();
                foreach (var test in DurationAnomalyTests)
                {
                    // test got slower
                    var color = _colorScheme.Error;
                    var direction = "+";
                    if (test.DurationChange.Ticks < 0)
                    {
                        // test got faster
                        color = _colorScheme.DarkSuccess;
                        direction = "";
                    }
                    str.Append($" {_bulletChar} ")
                        .Append(DisplayUtil.GetPrettyTestName(test.FullName, _colorScheme.DarkDefault, _colorScheme.Default, _colorScheme.DarkDefault))
                        .Append($"Diff", _colorScheme.Bright)
                        .Append($"{UTF8Constants.LeftBracket}", _colorScheme.Bright)
                        .Append($"{direction}{test.DurationChange.ToElapsedTime()}", color)
                        .Append($"{UTF8Constants.RightBracket}", _colorScheme.Bright)
                        .Append($" Time", _colorScheme.Bright)
                        .Append($"{UTF8Constants.LeftBracket}", _colorScheme.Bright)
                        .Append($"{test.Duration.ToElapsedTime()}", _colorScheme.Duration)
                        .Append($"{UTF8Constants.RightBracket}", _colorScheme.Bright)
                        .Append($" Median", _colorScheme.Bright)
                        .Append($"{UTF8Constants.LeftBracket}", _colorScheme.Bright)
                        .Append($"{test.Average.ToElapsedTime()}", _colorScheme.Duration)
                        .AppendLine($"{UTF8Constants.RightBracket}", _colorScheme.Bright);
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
