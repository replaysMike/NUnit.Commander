using AnyConsole;
using PInvoke;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NUnit.Commander.Display
{
    public static class ConsoleUtil
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        extern static bool GetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, ref CONSOLE_FONT_INFOEX lpConsoleCurrentFont);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CONSOLE_FONT_INFOEX
        {
            public uint cbSize;
            public uint nFont;
            public COORD dwFontSize;
            public int FontFamily;
            public int FontWeight;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
            public string FaceName;
        }

        [DllImport("gdi32.dll")]
        private static extern uint GetFontUnicodeRanges(IntPtr hdc, IntPtr lpgs);

        [DllImport("gdi32.dll")]
        private extern static IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        
        private enum StdHandle
        {
            OutputHandle = -11
        }

        private const int TMPF_TRUETYPE = 4;
        private const int LF_FACESIZE = 32;

        [DllImport("kernel32")]
        private static extern IntPtr GetStdHandle(StdHandle index);


        public struct FontCapabilities
        {
            public string FontName;
            public bool SupportsDesiredUnicodeRanges;
        }

        public static FontCapabilities GetCurrentFont()
        {
            var handle = GetStdHandle(StdHandle.OutputHandle);
            var info = new CONSOLE_FONT_INFOEX();
            info.cbSize = (uint)Marshal.SizeOf(info);
            GetCurrentConsoleFontEx(handle, false, ref info);
            var isTrueType = (info.FontFamily & TMPF_TRUETYPE) == TMPF_TRUETYPE;
            var fontName = info.FaceName;
            var font = new Font(fontName, 10);
            var ranges = GetUnicodeRangesForFont(font);
            // ‭10240‬, ‭10495‬
            var min = 0x2801;
            var max = 0x28FF;
            var test = CheckIfCharInFont('•', font);
            int unicodeValue = Convert.ToUInt16('\u2801');
            var supportsDesiredUnicodeRanges = ranges.Any(x => x.Low >= min && x.High <= min && (x.Low <= max && x.High >= max));

            return new FontCapabilities
            {
                FontName = fontName,
                SupportsDesiredUnicodeRanges = supportsDesiredUnicodeRanges
            };
        }

        public static bool CheckIfCharInFont(char character, Font font)
        {
            var intval = Convert.ToUInt16(character);
            var ranges = GetUnicodeRangesForFont(font);
            var isCharacterPresent = false;
            foreach (var range in ranges)
            {
                if (intval >= range.Low && intval <= range.High)
                {
                    isCharacterPresent = true;
                    break;
                }
            }
            return isCharacterPresent;
        }

        public static List<ExtendedConsole.FontRange> GetUnicodeRangesForFont(Font font)
        {
            var g = Graphics.FromHwnd(IntPtr.Zero);
            var hdc = g.GetHdc();
            var hFont = font.ToHfont();
            var old = SelectObject(hdc, hFont);
            var size = GetFontUnicodeRanges(hdc, IntPtr.Zero);
            var glyphSet = Marshal.AllocHGlobal((int)size);
            GetFontUnicodeRanges(hdc, glyphSet);
            var fontRanges = new List<ExtendedConsole.FontRange>();
            var count = Marshal.ReadInt32(glyphSet, 12);
            for (var i = 0; i < count; i++)
            {
                var range = new ExtendedConsole.FontRange();
                range.Low = (UInt16)Marshal.ReadInt16(glyphSet, 16 + i * 4);
                range.High = (UInt16)(range.Low + Marshal.ReadInt16(glyphSet, 18 + i * 4) - 1);
                fontRanges.Add(range);
            }
            SelectObject(hdc, old);
            Marshal.FreeHGlobal(glyphSet);
            g.ReleaseHdc(hdc);
            g.Dispose();
            return fontRanges;
        }
    }
}
