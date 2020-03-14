using Moq;
using NUnit.Commander.IO;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

namespace NUnit.Commander.Tests.IO
{
    [TestFixture]
    public class ReportContextTests
    {
        private MockRepository mockRepository;



        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);


        }

        private ReportContext CreateReportContext()
        {
            return new ReportContext();
        }

        [Test]
        public void ReportContext_Initializes()
        {
            // Arrange
            var reportContext = CreateReportContext();

            // Act

            // Assert
            Assert.NotNull(reportContext.EventEntries);
            Assert.NotNull(reportContext.FrameworkRuntimes);
            Assert.NotNull(reportContext.Frameworks);
            Assert.NotNull(reportContext.TestRunIds);
            Assert.NotNull(reportContext.Performance);
            mockRepository.VerifyAll();
        }
    }
}
