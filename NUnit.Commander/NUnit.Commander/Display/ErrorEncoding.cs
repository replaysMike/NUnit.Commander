using AnyConsole;
using System;
using System.Linq;

namespace NUnit.Commander.Display
{
    public static class ErrorEncoding
    {
        public static DetectedEncoding DetectEncoding(string text)
        {
            if (text.Contains("<html", StringComparison.InvariantCultureIgnoreCase)
                && text.Contains("</html>", StringComparison.InvariantCultureIgnoreCase))
                return DetectedEncoding.Html;

            if (text.Contains('{')
                && text.Contains('}')
                && text.Contains(':')
                // make sure its valid json or looks like it
                && (text.Count(c => c == '{') + text.Count(c => c == '}')) % 2 == 0)
                return DetectedEncoding.Json;

            return DetectedEncoding.Text;
        }

        public static ColorTextBuilder Format(string text, ColorScheme colorScheme)
        {
            var encoding = DetectEncoding(text);
            switch (encoding)
            {
                case DetectedEncoding.Html:
                    return HtmlPrettify.Format(text, colorScheme);
                case DetectedEncoding.Json:
                    return JsonPrettify.Format(text, colorScheme);
                case DetectedEncoding.Text:
                default:
                    return StackTracePrettify.Format(text, colorScheme);
            }
        }
    }

    public enum DetectedEncoding
    {
        Text,
        Html,
        Json
    }
}
