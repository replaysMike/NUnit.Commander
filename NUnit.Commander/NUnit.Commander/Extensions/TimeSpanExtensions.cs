using System;
using System.Text;

namespace NUnit.Commander.Extensions
{
    public static class TimeSpanExtensions
    {
        public static string ToElapsedTime(this TimeSpan ts)
        {
            if (ts.TotalHours >= 24.0)
                return $"{ts.TotalDays.ToString("F")} days";
            else if (ts.TotalHours >= 1.0)
                return $"{ts.TotalHours.ToString("F")} hours";
            else if (ts.TotalMinutes >= 60.0)
                return $"{ts.TotalMinutes.ToString("F")}min";
            else if (ts.TotalSeconds >= 1.0)
                return $"{ts.TotalSeconds.ToString("F")}s";

            // display in milliseconds
            return $"{ts.TotalMilliseconds.ToString("F")}ms";
        }

        public static string ToTotalElapsedTime(this TimeSpan ts)
        {
            var sb = new StringBuilder();
            if (ts.TotalDays >= 1.0)
                sb.Append($"{ts.Days} days ");
            if (ts.TotalHours >= 1.0)
                sb.Append($"{ts.Hours} hours ");
            if (ts.TotalMinutes >= 1.0)
                sb.Append($"{ts.Minutes} min ");
            if (ts.TotalSeconds >= 1.0)
                sb.Append($"{ts.Seconds} s ");
            if (ts.TotalMilliseconds >= 1.0)
                sb.Append($"{ts.Milliseconds} ms ");

            return sb.ToString();
        }
    }
}
