using System;

namespace NUnit.Commander.Configuration
{
    public static class Constants
    {
        public const string ApplicationName = "NUnit.Commander";
        public static readonly string Copyright = $"Copyright \u00A9 {DateTime.Now.Year} Refactor Software Inc.";
        public const string WebsiteUrl = "https://github.com/replaysMike/NUnit.Commander";
        public const string KeyboardHelp = "[Q] to quit run, [P] to pause display, [Tab] to change views";
        public const string SimpleSeparator = "============================";
        public const string TimeFormat = "hh:mm:ss.fff tt";
    }

    public static class UTF8Constants
    {
        public const char Bullet = '\u2022'; // bullet •
        public const char HorizontalLine = '\u2500'; // light dash '─'
        public const char BoxHorizontal = '═';
        public const char BoxVertical = '║';
        public const char BoxTopLeft = '╔';
        public const char BoxTopRight = '╗';
        public const char BoxBottomLeft = '╚';
        public const char BoxBottomRight = '╝';

        public const char RoundBoxHorizontal = '─';
        public const char RoundBoxVertical = '│';
        public const char RoundBoxTopLeft = '╭';
        public const char RoundBoxTopRight = '╮';
        public const char RoundBoxBottomLeft = '╰';
        public const char RoundBoxBottomRight = '╯';

        public const char LeftBracket = '[';
        public const char RightBracket = ']';
    }
}
