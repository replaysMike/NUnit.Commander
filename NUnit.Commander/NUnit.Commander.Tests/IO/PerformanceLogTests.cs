using Moq;
using NUnit.Commander.IO;
using NUnit.Framework;
using static NUnit.Commander.IO.PerformanceLog;

namespace NUnit.Commander.Tests.IO
{
    [TestFixture]
    public class PerformanceLogTests
    {
        private MockRepository mockRepository;



        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);


        }

        private PerformanceLog CreatePerformanceLog()
        {
            return new PerformanceLog();
        }

        [Test]
        public void AddEntry_MeasuresPeak_ExpectedBehavior()
        {
            // Arrange
            var performanceLog = CreatePerformanceLog();
            PerformanceType type = PerformanceType.CpuUsed;

            // Act
            performanceLog.AddEntry(type, 90);
            performanceLog.AddEntry(type, 100);
            var result = performanceLog.GetPeak(type);

            // Assert
            Assert.AreEqual(100, result);
            mockRepository.VerifyAll();
        }

        [Test]
        public void GetMedian_MeasuresMedian_ExpectedBehavior()
        {
            // Arrange
            var performanceLog = CreatePerformanceLog();
            PerformanceType type = PerformanceType.CpuUsed;
            float value = 100;

            // Act
            performanceLog.AddEntry(type, 100);
            performanceLog.AddEntry(type, 50);
            performanceLog.AddEntry(type, 75);
            var result = performanceLog.GetMedian(type);

            // Assert
            Assert.AreEqual(75.0, result);
            mockRepository.VerifyAll();
        }

        [Test]
        public void GetAverage_MeasuresAverage_ExpectedBehavior()
        {
            // Arrange
            var performanceLog = CreatePerformanceLog();
            PerformanceType type = PerformanceType.CpuUsed;

            // Act
            performanceLog.AddEntry(type, 100);
            performanceLog.AddEntry(type, 50);
            performanceLog.AddEntry(type, 75);
            var result = performanceLog.GetAverage(type);

            // Assert
            Assert.AreEqual(75.0, result);
            mockRepository.VerifyAll();
        }

        [Test]
        public void Dispose_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var performanceLog = CreatePerformanceLog();

            // Act
            performanceLog.Dispose();

            // Assert
            Assert.Pass();
            mockRepository.VerifyAll();
        }
    }
}
