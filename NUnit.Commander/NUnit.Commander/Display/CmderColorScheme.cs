using System.Drawing;

namespace NUnit.Commander.Display
{
    public class CmderColorScheme : IColorScheme
    {
        public Color? Background { get; set; }
        public Color Default { get; set; } = Color.Gray;
        public Color DarkDefault { get; set; } = Color.DarkSlateGray;
        public Color Bright { get; set; } = Color.White;
        public Color Error { get; set; } = Color.Red;
        public Color DarkError { get; set; } = Color.DarkRed;
        public Color Success { get; set; } = Color.Gray;
        public Color DarkSuccess { get; set; } = Color.Green;
        public Color Highlight { get; set; } = Color.White;
        public Color DarkHighlight { get; set; } = Color.FromArgb(128, 128, 0);
        public Color DarkHighlight2 { get; set; } = Color.FromArgb(64, 64, 0);
        public Color DarkHighlight3 { get; set; } = Color.FromArgb(32, 32, 0);
        public Color Duration { get; set; } = Color.Cyan;
        public Color DarkDuration { get; set; } = Color.DarkCyan;
    }
}
