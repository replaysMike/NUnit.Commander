//using Colorful;
using NUnit.Commander.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace NUnit.Commander.Display
{
    public class ColorScheme : IColorScheme
    {
        private readonly Dictionary<ConsoleColor, MappedColor> _colorMap = new Dictionary<ConsoleColor, MappedColor>();
        private Dictionary<string, COLORREF> _originalColorMap = new Dictionary<string, COLORREF>();
        private ColorMapper _mapper = new ColorMapper();

        public ColorSchemes ColorSchemeName { get; }
        public IColorScheme Colors { get; private set; }

        public Color? Background => Colors.Background;
        public Color RaisedBackground => Colors.RaisedBackground;
        public Color Default => Colors.Default;
        public Color DarkDefault => Colors.DarkDefault;
        public Color Bright => Colors.Bright;
        public Color Error => Colors.Error;
        public Color DarkError => Colors.DarkError;
        public Color Success => Colors.Success;
        public Color DarkSuccess => Colors.DarkSuccess;
        public Color Highlight => Colors.Highlight;
        public Color DarkHighlight => Colors.DarkHighlight;
        public Color DarkHighlight2 => Colors.DarkHighlight2;
        public Color DarkHighlight3 => Colors.DarkHighlight3;
        public Color Duration => Colors.Duration;
        public Color DarkDuration => Colors.DarkDuration;

        public ColorScheme(ColorSchemes colorScheme)
        {
            ColorSchemeName = colorScheme;

            MapExistingColors();
            // load the color scheme
            LoadColorScheme();
            MapColorScheme();
        }

        public Color GetColor(string name)
        {
            switch (name)
            {
                case "Default":
                    return Default;
                case "RaisedBackground":
                    return RaisedBackground;
                case "DarkDefault":
                    return DarkDefault;
                case "Bright":
                    return Bright;
                case "Error":
                    return Error;
                case "DarkError":
                    return DarkError;
                case "Success":
                    return Success;
                case "DarkSuccess":
                    return DarkSuccess;
                case "Highlight":
                    return Highlight;
                case "DarkHighlight":
                    return DarkHighlight;
                case "DarkHighlight2":
                    return DarkHighlight2;
                case "Duration":
                    return Duration;
                case "DarkDuration":
                    return DarkDuration;
                default:
                    return Default;
            }
        }

        private void LoadColorScheme()
        {
            switch (ColorSchemeName)
            {
                case ColorSchemes.Cmder:
                    Colors = new CmderColorScheme();
                    break;
                case ColorSchemes.Monochrome:
                    Colors = new MonochromeColorScheme();
                    break;
                case ColorSchemes.Default:
                default:
                    Colors = new DefaultColorScheme();
                    break;
            }
        }

        private void MapExistingColors()
        {
            if (!Console.IsOutputRedirected)
            {
                _originalColorMap = _mapper.GetBufferColors();
            }
        }

        private void MapColorScheme()
        {
            /*if (!Console.IsOutputRedirected)
                System.Console.ResetColor();
            System.Console.BackgroundColor = ConsoleColor.Black;
            System.Console.ForegroundColor = ConsoleColor.Gray;*/

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
            // _colorMap.Add(System.ConsoleColor.Magenta, new MappedColor(RaisedBackground, nameof(RaisedBackground)));
            // _colorMap.Add(System.ConsoleColor.DarkGreen, new MappedColor(DarkSuccess, nameof(Color.Magenta)));

            // map the colors
            if (!Console.IsOutputRedirected)
            {
                foreach (var map in _colorMap)
                    _mapper.MapColor(map.Key, map.Value.Color);
            }
        }

        /// <summary>
        /// Reset console colors to original state
        /// </summary>
        public void ResetColor()
        {
            if (!Console.IsOutputRedirected && _originalColorMap != null && _originalColorMap.Any())
            {
                _mapper.SetBatchBufferColors(_originalColorMap);
            }
        }

        /// <summary>
        /// Reset console background color to original state
        /// </summary>
        public void ResetBackgroundColor()
        {
            if (!Console.IsOutputRedirected)
            {
                _mapper.ResetBackgroundColor();
            }
        }

        /// <summary>
        /// Reset console foreground color to original state
        /// </summary>
        public void ResetForegroundColor()
        {
            if (!Console.IsOutputRedirected)
            {
                _mapper.ResetForegroundColor();
            }
        }

        /// <summary>
        /// Get a mapped console color for a given color
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public System.ConsoleColor? GetMappedConsoleColor(Color color)
        {
            if (_colorMap
                .Where(x => x.Value.Color == color)
                .Any())
            {
                var consoleColor = _colorMap
                    .Where(x => x.Value.Color == color)
                    .Select(x => x.Key)
                    .FirstOrDefault();
                return consoleColor;
            }
            return null;
        }

        /// <summary>
        /// Get a mapped color for a given console color
        /// </summary>
        /// <param name="consoleColor"></param>
        /// <returns></returns>
        public Color GetMappedColor(System.ConsoleColor consoleColor)
        {
            var mappedColor = _colorMap
                .Where(x => x.Key == consoleColor)
                .Select(x => x.Value.Color)
                .FirstOrDefault();
            return mappedColor;
        }

        public void PrintColorMap()
        {
            var block = "████████████████";

            System.Console.WriteLine($"Background - {System.Console.BackgroundColor}");
            System.Console.WriteLine($"Foreground - {System.Console.ForegroundColor}");
            var colors = _mapper.GetBufferColors();
            foreach (var color in colors)
            {
                var c = Color.FromArgb((int)color.Value.ColorDWORD);
                System.Console.Write($"Color: ");
                System.Console.WriteLine($"{color.Key} - {c.R},{c.B},{c.G} ({color.Value.ColorDWORD})");
            }
        }

        /// <summary>
        /// Print the color map to the console
        /// </summary>
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
            Console.WriteLine($"Color Type:     {Colors.GetType().FullName}");
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
