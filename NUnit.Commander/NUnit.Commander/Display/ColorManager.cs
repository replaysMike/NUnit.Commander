//using Colorful;
using NUnit.Commander.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ColorfulConsole = Colorful.Console;

namespace NUnit.Commander.Display
{
    public class ColorManager : IColorScheme
    {
        private IDictionary<ConsoleColor, MappedColor> _colorMap = new Dictionary<ConsoleColor, MappedColor>();
        public ColorSchemes ColorSchemeName { get; }
        public IColorScheme ColorScheme { get; private set; }

        public Color? Background => ColorScheme.Background;
        public Color Default => ColorScheme.Default;
        public Color DarkDefault => ColorScheme.DarkDefault;
        public Color Bright => ColorScheme.Bright;
        public Color Error => ColorScheme.Error;
        public Color DarkError => ColorScheme.DarkError;
        public Color Success => ColorScheme.Success;
        public Color DarkSuccess => ColorScheme.DarkSuccess;
        public Color Highlight => ColorScheme.Highlight;
        public Color DarkHighlight => ColorScheme.DarkHighlight;
        public Color DarkHighlight2 => ColorScheme.DarkHighlight2;
        public Color DarkHighlight3 => ColorScheme.DarkHighlight3;
        public Color Duration => ColorScheme.Duration;
        public Color DarkDuration => ColorScheme.DarkDuration;

        public ColorManager(ColorSchemes colorScheme)
        {
            ColorSchemeName = colorScheme;
            // load the color scheme
            LoadColorScheme();
            if (!Console.IsOutputRedirected)
                MapColorScheme();
        }

        private void LoadColorScheme()
        {
            switch (ColorSchemeName)
            {
                case ColorSchemes.Cmder:
                    ColorScheme = new CmderColorScheme();
                    break;
                case ColorSchemes.Monochrome:
                    ColorScheme = new MonochromeColorScheme();
                    break;
                case ColorSchemes.Default:
                default:
                    ColorScheme = new DefaultColorScheme();
                    break;
            }
        }

        private void MapColorScheme()
        {
            var mapper = new ColorMapper();
            System.Console.ResetColor();
            System.Console.BackgroundColor = ConsoleColor.Black;
            System.Console.ForegroundColor = ConsoleColor.Gray;

            if (Background.HasValue)
                _colorMap.Add(System.ConsoleColor.Black, new MappedColor(Background.Value, nameof(Background)));
            else
                _colorMap.Add(System.ConsoleColor.Black, new MappedColor(Color.Black, nameof(Color.Black)));
            _colorMap.Add(System.ConsoleColor.Blue, new MappedColor(Duration, nameof(Duration)));
            _colorMap.Add(System.ConsoleColor.DarkCyan, new MappedColor(DarkDuration, nameof(DarkDuration)));
            _colorMap.Add(System.ConsoleColor.DarkGray, new MappedColor(DarkDefault, nameof(DarkDefault)));
            //_colorMap.Add(System.ConsoleColor.DarkGreen, new MappedColor(DarkSuccess, nameof(DarkSuccess)));
            _colorMap.Add(System.ConsoleColor.DarkMagenta, new MappedColor(DarkSuccess, nameof(DarkSuccess)));
            _colorMap.Add(System.ConsoleColor.DarkRed, new MappedColor(DarkError, nameof(DarkError)));
            _colorMap.Add(System.ConsoleColor.DarkYellow, new MappedColor(DarkHighlight, nameof(DarkHighlight)));
            _colorMap.Add(System.ConsoleColor.Gray, new MappedColor(Default, nameof(Default)));
            _colorMap.Add(System.ConsoleColor.Green, new MappedColor(Success, nameof(Success)));
            _colorMap.Add(System.ConsoleColor.Red, new MappedColor(Error, nameof(Error)));
            _colorMap.Add(System.ConsoleColor.White, new MappedColor(Bright, nameof(Bright)));
            _colorMap.Add(System.ConsoleColor.Yellow, new MappedColor(Highlight, nameof(Highlight)));
            _colorMap.Add(System.ConsoleColor.DarkBlue, new MappedColor(DarkHighlight2, nameof(DarkHighlight2)));
            _colorMap.Add(System.ConsoleColor.Cyan, new MappedColor(DarkHighlight3, nameof(DarkHighlight3)));

            // unused colors
            _colorMap.Add(System.ConsoleColor.Magenta, new MappedColor(Color.Magenta, nameof(Color.Magenta)));
            // _colorMap.Add(System.ConsoleColor.DarkGreen, new MappedColor(DarkSuccess, nameof(Color.Magenta)));

            // map the colors
            foreach (var map in _colorMap)
                mapper.MapColor(map.Key, map.Value.Color);
        }

        public System.ConsoleColor GetMappedConsoleColor(Color color)
        {
            var consoleColor = _colorMap
                .Where(x => x.Value.Color == color)
                .Select(x => x.Key)
                .FirstOrDefault();
            return consoleColor;
        }

        public Color GetMappedColor(System.ConsoleColor consoleColor)
        {
            var mappedColor = _colorMap
                .Where(x => x.Key == consoleColor)
                .Select(x => x.Value.Color)
                .FirstOrDefault();
            return mappedColor;
        }

        public void PrintColorsToConsole()
        {
            var block = "████████████████";

            var colors = (System.ConsoleColor[])System.ConsoleColor.GetValues(typeof(System.ConsoleColor));
            foreach (var color in colors)
            {
                Console.ResetColor();
                System.Console.Write($"Color: ");
                System.Console.ForegroundColor = color;
                System.Console.WriteLine($"{block} - {color.ToString()}");
            }

            Console.WriteLine();
            Console.ResetColor();
            Console.WriteLine($"Color Scheme:   {ColorSchemeName}");
            Console.WriteLine($"Color Type:     {ColorScheme.GetType().FullName}");
            foreach (var mappedColor in _colorMap)
            {
                Console.ResetColor();
                Console.Write($"{mappedColor.Value.Name, -15}");
                Console.ForegroundColor = mappedColor.Key;
                Console.WriteLine($"{block} - {mappedColor.Value.Color.Name}");
            }

            Console.ResetColor();
        }

        public struct MappedColor
        {
            public Color Color { get; set; }
            public string Name { get; set; }
            public MappedColor(Color color, string name)
            {
                Color = color;
                Name = name;
            }

            public override string ToString()
            {
                return $"{Name} = {Color.Name}";
            }
        }
    }
}
