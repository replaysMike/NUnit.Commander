﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace NUnit.Commander.Display
{
    /// <summary>
    /// Exposes methods used for mapping System.Drawing.Colors to System.ConsoleColors.
    /// </summary>
    public class ColorMapper
    {
        private volatile byte _currentColors;
        private volatile byte _currentForeground;
        private volatile byte _currentBackground;
        private volatile byte _defaultColors;
        private volatile byte _defaultForeground;
        private volatile byte _defaultBackground;
        private bool _haveReadDefaultColors = false;

        [Flags]
        internal enum InternalColor : short
        {
            Black = 0,
            ForegroundBlue = 0x1,
            ForegroundGreen = 0x2,
            ForegroundRed = 0x4,
            ForegroundYellow = 0x6,
            ForegroundIntensity = 0x8,
            BackgroundBlue = 0x10,
            BackgroundGreen = 0x20,
            BackgroundRed = 0x40,
            BackgroundYellow = 0x60,
            BackgroundIntensity = 0x80,

            ForegroundMask = 0xf,
            BackgroundMask = 0xf0,
            ColorMask = 0xff
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            internal short X;
            internal short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SMALL_RECT
        {
            internal short Left;
            internal short Top;
            internal short Right;
            internal short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CONSOLE_SCREEN_BUFFER_INFO_EX
        {
            internal int cbSize;
            internal COORD dwSize;
            internal COORD dwCursorPosition;
            internal ushort wAttributes;
            internal SMALL_RECT srWindow;
            internal COORD dwMaximumWindowSize;
            internal ushort wPopupAttributes;
            internal bool bFullscreenSupported;
            internal COLORREF black;
            internal COLORREF darkBlue;
            internal COLORREF darkGreen;
            internal COLORREF darkCyan;
            internal COLORREF darkRed;
            internal COLORREF darkMagenta;
            internal COLORREF darkYellow;
            internal COLORREF gray;
            internal COLORREF darkGray;
            internal COLORREF blue;
            internal COLORREF green;
            internal COLORREF cyan;
            internal COLORREF red;
            internal COLORREF magenta;
            internal COLORREF yellow;
            internal COLORREF white;
        }

        private const int STD_OUTPUT_HANDLE = -11;                               // per WinBase.h
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);    // per WinBase.h

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, ushort wAttributes);

        /// <summary>
        /// Maps a System.Drawing.Color to a System.ConsoleColor.
        /// </summary>
        /// <param name="oldColor">The color to be replaced.</param>
        /// <param name="newColor">The color to be mapped.</param>
        public void MapColor(ConsoleColor oldColor, Color newColor)
        {
            MapColor(oldColor, newColor.R, newColor.G, newColor.B);
        }

        /// <summary>
        /// Gets a collection of all 16 colors in the console buffer.
        /// </summary>
        /// <returns>Returns all 16 COLORREFs in the console buffer as a dictionary keyed by the COLORREF's alias in the buffer's ColorTable.</returns>
        public Dictionary<string, COLORREF> GetBufferColors()
        {
            var colors = new Dictionary<string, COLORREF>();
            var hConsoleOutput = GetStdHandle(STD_OUTPUT_HANDLE);    // 7
            var csbe = GetBufferInfo(hConsoleOutput);

            colors.Add("black", csbe.black);
            colors.Add("darkBlue", csbe.darkBlue);
            colors.Add("darkGreen", csbe.darkGreen);
            colors.Add("darkCyan", csbe.darkCyan);
            colors.Add("darkRed", csbe.darkRed);
            colors.Add("darkMagenta", csbe.darkMagenta);
            colors.Add("darkYellow", csbe.darkYellow);
            colors.Add("gray", csbe.gray);
            colors.Add("darkGray", csbe.darkGray);
            colors.Add("blue", csbe.blue);
            colors.Add("green", csbe.green);
            colors.Add("cyan", csbe.cyan);
            colors.Add("red", csbe.red);
            colors.Add("magenta", csbe.magenta);
            colors.Add("yellow", csbe.yellow);
            colors.Add("white", csbe.white);

            return colors;
        }

        /// <summary>
        /// Reset the foreground color only
        /// </summary>
        public void ResetForegroundColor()
        {
            var hConsoleOutput = GetStdHandle(STD_OUTPUT_HANDLE); // 7
            var csbe = GetBufferInfo(hConsoleOutput);

            var currentDefaultAttrs = (ushort)_currentColors;
            var currentForeground = _currentForeground;
            var currentBackground = _currentBackground;

            var defaultAttrs = (ushort)_defaultColors;
            var defaultForeground = _defaultForeground;
            var defaultBackground = _defaultBackground;
            if (currentForeground != defaultForeground)
            {
                // reset only the foreground bit
                var resetAttrs = (ushort)((ushort)currentBackground | defaultForeground);
                SetConsoleTextAttribute(hConsoleOutput, resetAttrs);
            }
        }

        /// <summary>
        /// Reset the background color only
        /// </summary>
        public void ResetBackgroundColor()
        {
            var hConsoleOutput = GetStdHandle(STD_OUTPUT_HANDLE); // 7
            var csbe = GetBufferInfo(hConsoleOutput);

            var currentDefaultAttrs = (ushort)_currentColors;
            var currentForeground = _currentForeground;
            var currentBackground = _currentBackground;

            var defaultAttrs = (ushort)_defaultColors;
            var defaultForeground = _defaultForeground;
            var defaultBackground = _defaultBackground;
            if (currentBackground != defaultBackground)
            {
                // reset only the background bit
                System.Diagnostics.Debug.WriteLine($"Background: {currentBackground},{defaultBackground}");
                var resetAttrs = (ushort)((ushort)currentForeground | defaultBackground);
                SetConsoleTextAttribute(hConsoleOutput, resetAttrs);
            }
        }

        /// <summary>
        /// Sets all 16 colors in the console buffer using colors supplied in a dictionary.
        /// </summary>
        /// <param name="colors">A dictionary containing COLORREFs keyed by the COLORREF's alias in the buffer's ColorTable.</param>
        public void SetBatchBufferColors(Dictionary<string, COLORREF> colors)
        {
            var hConsoleOutput = GetStdHandle(STD_OUTPUT_HANDLE); // 7
            var csbe = GetBufferInfo(hConsoleOutput);

            csbe.black = colors["black"];
            //csbe.black = new COLORREF(255, 0, 0);
            csbe.darkBlue = colors["darkBlue"];
            csbe.darkGreen = colors["darkGreen"];
            csbe.darkCyan = colors["darkCyan"];
            csbe.darkRed = colors["darkRed"];
            csbe.darkMagenta = colors["darkMagenta"];
            csbe.darkYellow = colors["darkYellow"];
            csbe.gray = colors["gray"];
            //csbe.gray = new COLORREF(255, 0, 0);
            csbe.darkGray = colors["darkGray"];
            csbe.blue = colors["blue"];
            csbe.green = colors["green"];
            csbe.cyan = colors["cyan"];
            csbe.red = colors["red"];
            csbe.magenta = colors["magenta"];
            csbe.yellow = colors["yellow"];
            csbe.white = colors["white"];

            SetBufferInfo(hConsoleOutput, csbe);
        }

        private CONSOLE_SCREEN_BUFFER_INFO_EX GetBufferInfo(IntPtr hConsoleOutput)
        {
            var csbe = new CONSOLE_SCREEN_BUFFER_INFO_EX();
            csbe.cbSize = (int)Marshal.SizeOf(csbe); // 96 = 0x60

            if (hConsoleOutput == INVALID_HANDLE_VALUE)
            {
                throw CreateException(Marshal.GetLastWin32Error());
            }

            var brc = GetConsoleScreenBufferInfoEx(hConsoleOutput, ref csbe);

            if (!brc)
            {
                throw CreateException(Marshal.GetLastWin32Error());
            }

            _currentColors = (byte)(csbe.wAttributes & (ushort)InternalColor.ColorMask);
            _currentForeground = (byte)(csbe.wAttributes & (ushort)InternalColor.ForegroundMask);
            _currentBackground = (byte)(csbe.wAttributes & (ushort)InternalColor.BackgroundMask);

            if (!_haveReadDefaultColors)
            {
                _defaultColors = (byte)(csbe.wAttributes & (ushort)InternalColor.ColorMask);
                _defaultForeground = (byte)(csbe.wAttributes & (ushort)InternalColor.ForegroundMask);
                _defaultBackground = (byte)(csbe.wAttributes & (ushort)InternalColor.BackgroundMask);
                _haveReadDefaultColors = true;
            }

            return csbe;
        }

        public void MapColor(ConsoleColor color, uint r, uint g, uint b)
        {
            var hConsoleOutput = GetStdHandle(STD_OUTPUT_HANDLE); // 7
            var csbe = GetBufferInfo(hConsoleOutput);

            switch (color)
            {
                case ConsoleColor.Black:
                    csbe.black = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.DarkBlue:
                    csbe.darkBlue = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.DarkGreen:
                    csbe.darkGreen = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.DarkCyan:
                    csbe.darkCyan = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.DarkRed:
                    csbe.darkRed = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.DarkMagenta:
                    csbe.darkMagenta = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.DarkYellow:
                    csbe.darkYellow = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.Gray:
                    csbe.gray = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.DarkGray:
                    csbe.darkGray = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.Blue:
                    csbe.blue = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.Green:
                    csbe.green = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.Cyan:
                    csbe.cyan = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.Red:
                    csbe.red = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.Magenta:
                    csbe.magenta = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.Yellow:
                    csbe.yellow = new COLORREF(r, g, b);
                    break;
                case ConsoleColor.White:
                    csbe.white = new COLORREF(r, g, b);
                    break;
            }

            SetBufferInfo(hConsoleOutput, csbe);
        }

        private void SetBufferInfo(IntPtr hConsoleOutput, CONSOLE_SCREEN_BUFFER_INFO_EX csbe)
        {
            csbe.srWindow.Bottom++;
            csbe.srWindow.Right++;

            var brc = SetConsoleScreenBufferInfoEx(hConsoleOutput, ref csbe);
            if (!brc)
            {
                throw CreateException(Marshal.GetLastWin32Error());
            }
        }

        private Exception CreateException(int errorCode)
        {
            const int ERROR_INVALID_HANDLE = 6;
            if (errorCode == ERROR_INVALID_HANDLE) // Raised if the console is being run via another application, for example.
            {
                return new Exception();
            }

            return new ColorMappingException(errorCode);
        }
    }

    /// <summary>
    /// A Win32 COLORREF, used to specify an RGB color.  See MSDN for more information:
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/dd183449(v=vs.85).aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct COLORREF
    {
        public uint ColorDWORD;

        internal COLORREF(Color color)
        {
            ColorDWORD = (uint)color.R + (((uint)color.G) << 8) + (((uint)color.B) << 16);
        }

        internal COLORREF(uint r, uint g, uint b)
        {
            ColorDWORD = r + (g << 8) + (b << 16);
        }

        public override string ToString()
        {
            return ColorDWORD.ToString();
        }
    }

    /// <summary>
    /// Encapsulates information relating to exceptions thrown during color mapping.
    /// </summary>
    public sealed class ColorMappingException : Exception
    {
        /// <summary>
        /// The underlying Win32 error code associated with the exception that
        /// has been trapped.
        /// </summary>
        public int ErrorCode { get; private set; }

        /// <summary>
        /// Encapsulates information relating to exceptions thrown during color mapping.
        /// </summary>
        /// <param name="errorCode">The underlying Win32 error code associated with the exception that
        /// has been trapped.</param>
        public ColorMappingException(int errorCode)
            : base(string.Format("Color conversion failed with system error code {0}!", errorCode))
        {
            ErrorCode = errorCode;
        }
    }
}
