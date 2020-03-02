using System.Collections.Generic;

namespace NUnit.Commander.Models
{
    public class DataReport
    {
        public int TotalTests { get; set; }

        public ICollection<TestCaseReport> TestReports { get; set; } = new List<TestCaseReport>();

        public DataReport() { }
        public DataReport(DataReport dataReport)
        {
            TotalTests = dataReport.TotalTests;
            if (dataReport.TestReports != null)
            {
                foreach (var testReport in dataReport.TestReports)
                {
                    TestReports.Add(new TestCaseReport(testReport));
                }
            }
        }
    }
}
