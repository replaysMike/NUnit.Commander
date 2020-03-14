using AnyConsole;
using Moq;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.IO;
using NUnit.Commander.Models;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

namespace NUnit.Commander.Tests.IO
{
    [TestFixture]
    public class ReportWriterTests
    {
        private MockRepository mockRepository;

        private Mock<LogFriendlyConsole> mockExtendedConsole;
        private Mock<ColorManager> mockColorManager;
        private Mock<ApplicationConfiguration> mockApplicationConfiguration;
        private Mock<RunContext> mockRunContext;

        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);

            mockColorManager = mockRepository.Create<ColorManager>(ColorSchemes.Default);
            mockExtendedConsole = mockRepository.Create<LogFriendlyConsole>(false, mockColorManager.Object);
            mockApplicationConfiguration = mockRepository.Create<ApplicationConfiguration>();
            mockRunContext = mockRepository.Create<RunContext>();
        }

        private ReportWriter CreateReportWriter()
        {
            return new ReportWriter(mockExtendedConsole.Object, mockColorManager.Object, mockApplicationConfiguration.Object, mockRunContext.Object);
        }

        [Test]
        public void WriteFinalReport_NoExceptions_ExpectedBehavior()
        {
            // Arrange
            var reportWriter = CreateReportWriter();

            // Act
            reportWriter.WriteFinalReport();

            // Assert
            Assert.Pass();
            mockRepository.VerifyAll();
        }
    }
}
