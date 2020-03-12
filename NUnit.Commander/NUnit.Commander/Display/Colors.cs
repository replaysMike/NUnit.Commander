using Colorful;
using NUnit.Commander.Configuration;
using System.Drawing;

namespace NUnit.Commander.Display
{
    public class Colors : IColorScheme
    {
        public ColorSchemes ColorSchemeName { get; }
        public IColorScheme ColorSchema { get; private set; }
        public Color? Background => ColorSchema.Background;
        public Color Default => ColorSchema.Default;
        public Color DarkDefault => ColorSchema.DarkDefault;
        public Color Bright => ColorSchema.Bright;
        public Color Error => ColorSchema.Error;
        public Color DarkError => ColorSchema.DarkError;
        public Color Success => ColorSchema.Success;
        public Color DarkSuccess => ColorSchema.DarkSuccess;
        public Color Highlight => ColorSchema.Highlight;
        public Color DarkHighlight => ColorSchema.DarkHighlight;
        public Color DarkHighlight2 => ColorSchema.DarkHighlight2;
        public Color DarkHighlight3 => ColorSchema.DarkHighlight3;
        public Color Duration => ColorSchema.Duration;
        public Color DarkDuration => ColorSchema.DarkDuration;

        public Colors(ColorSchemes colorScheme)
        {
            ColorSchemeName = colorScheme;
            // load the color scheme
            LoadColorScheme();
            MapColorScheme();
        }

        private void LoadColorScheme()
        {
            switch (ColorSchemeName)
            {
                case ColorSchemes.Cmder:
                    ColorSchema = new CmderColorScheme();
                    break;
                case ColorSchemes.Default:
                default:
                    ColorSchema = new DefaultColorScheme();
                    break;
            }
        }

        private void MapColorScheme()
        {
            //Console.ReplaceAllColorsWithDefaults();   
            //Console.ReplaceColor(Color.Cyan, Color.Yellow);
        }

        public void PrintColorsToConsole()
        {
            var block = "████████████████";

            var colors = (System.ConsoleColor[])System.ConsoleColor.GetValues(typeof(System.ConsoleColor));
            Console.WriteLine($"System color palette");
            foreach (var color in colors)
            {
                System.Console.ForegroundColor = color;
                System.Console.WriteLine($"Color: {block} - {color.ToString()}");
            }

            Console.WriteLine();
            Console.ResetColor();
            Console.WriteLine($"Color Scheme:   {ColorSchemeName}");
            Console.WriteLine($"Color Type:     {ColorSchema.GetType().FullName}");
            Console.Write("Default:        ");
            Console.WriteLine($"{block} - {Default.ToString()}", Default);
            Console.Write("Background:     ");
            if (Background.HasValue)
                Console.WriteLine($"{block} - {Background.Value.ToString()}", Background.Value);
            else
                Console.WriteLine($"{block} - {Console.BackgroundColor.ToString()}", Console.BackgroundColor);
            Console.Write("DarkDefault:    ");
            Console.WriteLine($"{block} - {DarkDefault.ToString()}", DarkDefault);
            Console.Write("Bright:         ");
            Console.WriteLine($"{block} - {Bright.ToString()}", Bright);
            Console.Write("Error:          ");
            Console.WriteLine($"{block} - {Error.ToString()}", Error);
            Console.Write("DarkError:      ");
            Console.WriteLine($"{block} - {DarkError.ToString()}", DarkError);
            Console.Write("Success:        ");
            Console.WriteLine($"{block} - {Success.ToString()}", Success);
            Console.Write("DarkSuccess:    ");
            Console.WriteLine($"{block} - {DarkSuccess.ToString()}", DarkSuccess);
            Console.Write("Highlight:      ");
            Console.WriteLine($"{block} - {Highlight.ToString()}", Highlight);
            Console.Write("DarkHighlight:  ");
            Console.WriteLine($"{block} - {DarkHighlight.ToString()}", DarkHighlight);
            Console.Write("DarkHighlight2: ");
            Console.WriteLine($"{block} - {DarkHighlight2.ToString()}", DarkHighlight2);
            Console.Write("DarkHighlight3: ");
            Console.WriteLine($"{block} - {DarkHighlight3.ToString()}", DarkHighlight3);
            Console.Write("Duration:       ");
            Console.WriteLine($"{block} - {Duration.ToString()}", Duration);
            Console.Write("DarkDuration:   ");
            Console.WriteLine($"{block} - {DarkDuration.ToString()}", DarkDuration);
            Console.ResetColor();
        }
    }
}
