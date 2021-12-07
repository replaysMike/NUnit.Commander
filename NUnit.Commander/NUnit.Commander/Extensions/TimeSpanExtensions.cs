using System;
using System.Text;

namespace NUnit.Commander.Extensions
{
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Get the elapsed time summary (scaled to the time unit)
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static string ToElapsedTime(this TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return $"{ts.TotalDays.ToString("F")} days";
            else if (ts.TotalHours >= 1)
                return $"{ts.TotalHours.ToString("F")} hours";
            else if (ts.TotalMinutes >= 1)
                return $"{ts.TotalMinutes.ToString("F")}min";
            else if (ts.TotalSeconds >= 1)
                return $"{ts.TotalSeconds.ToString("F")}s";

            // display in milliseconds
            return $"{ts.TotalMilliseconds.ToString("F")}ms";
        }

        /// <summary>
        /// Get the total elapsed time summary (all time elements)
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static string ToTotalElapsedTime(this TimeSpan ts)
        {
            if(ts == TimeSpan.Zero)
                return $"0 seconds";
            var sb = new StringBuilder();
            if (ts.TotalDays >= 1.0)
                sb.Append($"{ts.Days} days ");
            if (ts.TotalHours >= 1.0)
                sb.Append($"{ts.Hours} hours ");
            if (ts.TotalMinutes >= 1.0)
                sb.Append($"{ts.Minutes}min ");
            if (ts.TotalSeconds >= 1.0 && ts.TotalSeconds < 60)
                sb.Append($"{ts.TotalSeconds.ToString("F")}s ");
            if (ts.TotalSeconds >= 1.0 && ts.TotalSeconds >= 60)
                sb.Append($"{ts.Seconds}s ");
            if (ts.TotalMilliseconds >= 1.0 && ts.TotalSeconds < 1)
                sb.Append($"{ts.TotalMilliseconds.ToString("F")}ms ");

            return sb.ToString();
        }
    }
}
