namespace NUnit.Commander.Configuration
{
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

        // use braille UTF-8 dots to show a 4x2 running animation
        public static readonly char[] BrailleRunningAnim = new char[] { '\u2801', '\u2802', '\u2804', '\u2840', '\u2880', '\u2820', '\u2810', '\u2808' };
        // use box drawing to show a running animation
        //public static readonly char[] AsciiRunningAnim = new char[] { '\u2574', '\u2577', '\u2576', '\u2575' };
        public static readonly char[] AsciiRunningAnim = new char[] { '\u2219', '\u2022', '\u25E6', '\u25CB' };
    }
}
