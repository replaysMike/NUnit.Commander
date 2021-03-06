﻿using System;

namespace NUnit.Commander.Configuration
{
    public static class Constants
    {
        public const string ApplicationName = "NUnit.Commander";
        public const string ExtensionName = "NUnit.Extension.TestMonitor";
        public const string ExtensionUrl = "https://github.com/replaysMike/NUnit.Extension.TestMonitor";
        public static readonly string Copyright = $"Copyright \u00A9 {DateTime.Now.Year} Refactor Software Inc.";
        public const string WebsiteUrl = "https://github.com/replaysMike/NUnit.Commander";
        public const string KeyboardHelp = "[Q] to quit run, [P] to pause display, [Tab] to change views";
        public const string SimpleSeparator = "============================";
        public const string TimeFormat = "hh:mm:ss.fff tt";
    }
}
