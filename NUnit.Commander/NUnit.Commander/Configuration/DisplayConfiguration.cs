namespace NUnit.Commander.Configuration
{
    public class DisplayConfiguration
    {
        /// <summary>
        /// True if ConEmu is detected
        /// </summary>
        public bool IsConEmuDetected { get; set; }

        /// <summary>
        /// True if Powershell is detected
        /// </summary>
        public bool IsPowershellDetected { get; set; }

        /// <summary>
        /// True if Command Prompt is detected
        /// </summary>
        public bool IsCommandPromptDetected { get; set; }

        /// <summary>
        /// True if console font supports extended unicode
        /// </summary>
        public bool SupportsExtendedUnicode { get; set; }
    }
}
