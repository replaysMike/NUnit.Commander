using Moq;
using NUnit.Commander.Configuration;
using NUnit.Commander.Display;
using NUnit.Framework;

namespace NUnit.Commander.Tests
{
    [TestFixture]
    public class CommanderTests
    {
        private MockRepository mockRepository;

        private Mock<ApplicationConfiguration> mockApplicationConfiguration;

        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);

            mockApplicationConfiguration = mockRepository.Create<ApplicationConfiguration>();
        }

        private Commander CreateCommander()
        {
            return new Commander(
                mockApplicationConfiguration.Object, new ColorScheme(ColorSchemes.Default));
        }

        [Test]
        public void Close_Closes_ExpectedBehavior()
        {
            // Arrange
            var commander = CreateCommander();

            // Act
            commander.Close();

            // Assert
            Assert.Pass();
            mockRepository.VerifyAll();
        }

        [Test]
        public void GenerateReportContext_ExpectedBehavior()
        {
            // Arrange
            var commander = CreateCommander();

            // Act
            var result = commander.GenerateReportContext();

            // Assert
            Assert.NotNull(result);
            mockRepository.VerifyAll();
        }

        [Test]
        public void Dispose_ExpectedBehavior()
        {
            // Arrange
            var commander = CreateCommander();

            // Act
            commander.Dispose();

            // Assert
            Assert.Pass();
            mockRepository.VerifyAll();
        }
    }
}
