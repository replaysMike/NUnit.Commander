using NUnit.Commander.Configuration;
using System.Drawing;

namespace NUnit.Commander.Display
{
    public class Colors : IColorScheme
    {
        public ColorSchemes ColorSchemeName { get; }
        public IColorScheme Color { get; private set; }
        public Color? Background => Color.Background;
        public Color Default => Color.Default;
        public Color DarkDefault => Color.DarkDefault;
        public Color Bright => Color.Bright;
        public Color Error => Color.Error;
        public Color DarkError => Color.DarkError;
        public Color Success => Color.Success;
        public Color DarkSuccess => Color.DarkSuccess;
        public Color Highlight => Color.Highlight;
        public Color DarkHighlight => Color.DarkHighlight;
        public Color DarkHighlight2 => Color.DarkHighlight2;
        public Color DarkHighlight3 => Color.DarkHighlight3;
        public Color Duration => Color.Duration;
        public Color DarkDuration => Color.DarkDuration;

        public Colors(ColorSchemes colorScheme)
        {
            ColorSchemeName = colorScheme;
            // load the color scheme
            LoadColorScheme();
        }

        private void LoadColorScheme()
        {
            switch (ColorSchemeName)
            {
                case ColorSchemes.Cmder:
                    Color = new CmderColorScheme();
                    break;
                case ColorSchemes.Default:
                default:
                    Color = new DefaultColorScheme();
                    break;
            }
        }
    }
}
