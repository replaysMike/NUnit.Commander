using Moq;
using NUnit.Commander.Analysis;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Commander.IO;
using NUnit.Framework;
using System.Collections.Generic;

namespace NUnit.Commander.Tests.Analysis
{
    [TestFixture]
    public class TestHistoryAnalyzerTests
    {
        private MockRepository mockRepository;

        private Mock<ApplicationConfiguration> mockApplicationConfiguration;
        private Mock<ColorScheme> mockColorScheme;
        private Mock<TestHistoryDatabaseProvider> mockTestHistoryDatabaseProvider;

        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);

            mockApplicationConfiguration = mockRepository.Create<ApplicationConfiguration>();
            mockColorScheme = mockRepository.Create<ColorScheme>();
            mockTestHistoryDatabaseProvider = mockRepository.Create<TestHistoryDatabaseProvider>(mockApplicationConfiguration.Object);
        }

        private TestHistoryAnalyzer CreateTestHistoryAnalyzer()
        {
            return new TestHistoryAnalyzer(
                mockApplicationConfiguration.Object,
                mockColorScheme.Object,
                mockTestHistoryDatabaseProvider.Object);
        }

        [Test]
        public void Analyze_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var testHistoryAnalyzer = this.CreateTestHistoryAnalyzer();
            var currentRun = new List<Models.TestHistoryEntry>();

            // Act
            var result = testHistoryAnalyzer.Analyze(currentRun);

            // Assert
            Assert.NotNull(result);
            mockRepository.VerifyAll();
        }
    }
}
