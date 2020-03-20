using System;
using System.Collections.Generic;
using System.Text;

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
}
