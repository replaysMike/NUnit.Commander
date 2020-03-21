namespace NUnit.Commander.Models
{
    public enum EventNames
    {
        None = 0,
        StartRun,
        StartAssembly,
        EndAssembly,
        StartSuite,
        EndSuite,
        StartTestFixture,
        EndTestFixture,
        StartTest,
        EndTest,
        Report
    }
}
