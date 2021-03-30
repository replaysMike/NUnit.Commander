namespace NUnit.Commander.Configuration
{
    public enum TestRunner
    {
        /// <summary>
        /// Specifies to use the NUnit-Console runner
        /// </summary>
        NUnitConsole,
        /// <summary>
        /// Specifies to use dotnet test
        /// </summary>
        DotNetTest,
        /// <summary>
        /// Automatically select the correct NUnit test runner(s) based on the test assemblies specified
        /// </summary>
        Auto
    }
}
