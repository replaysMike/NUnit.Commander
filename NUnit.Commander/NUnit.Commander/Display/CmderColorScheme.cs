using System.Drawing;

namespace NUnit.Commander.Display
{
    // Cmder Monokai theme
    public class CmderColorScheme : IColorScheme
    {
        public Color? Background { get; set; }
        public Color RaisedBackground { get; set; } = Color.FromArgb(16, 16, 16);
        public Color Default { get; set; } = Color.FromArgb(202, 202, 202);
        public Color DarkDefault { get; set; } = Color.FromArgb(124, 124, 124);
        public Color Bright { get; set; } = Color.White;
        public Color Error { get; set; } = Color.FromArgb(243, 4, 75);
        public Color DarkError { get; set; } = Color.FromArgb(167, 3, 52);
        public Color Success { get; set; } = Color.FromArgb(141, 208, 6);
        public Color DarkSuccess { get; set; } = Color.FromArgb(116, 170, 4);
        public Color Highlight { get; set; } = Color.FromArgb(182, 182, 73);
        public Color DarkHighlight { get; set; } = Color.FromArgb(204, 204, 129);
        public Color DarkHighlight2 { get; set; } = Color.FromArgb(1, 84, 158);
        public Color DarkHighlight3 { get; set; } = Color.FromArgb(88, 194, 229);
        public Color Duration { get; set; } = Color.FromArgb(3, 131, 245);
        public Color DarkDuration { get; set; } = Color.FromArgb(26, 131, 166);
    }
}
