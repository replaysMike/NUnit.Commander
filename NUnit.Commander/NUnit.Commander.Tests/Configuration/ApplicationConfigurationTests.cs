using Moq;
using NUnit.Commander.Configuration;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

namespace NUnit.Commander.Tests.Configuration
{
    [TestFixture]
    public class ApplicationConfigurationTests
    {
        private MockRepository mockRepository;
        private Mock<ApplicationConfiguration> mockApplicationConfiguration;


        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);
            mockApplicationConfiguration = mockRepository.Create<ApplicationConfiguration>();
        }

        private ApplicationConfiguration CreateApplicationConfiguration()
        {
            return new ApplicationConfiguration();
        }

        [Test]
        public void TestMethod1()
        {
            // Arrange
            var applicationConfiguration = CreateApplicationConfiguration();

            // Act


            // Assert
            Assert.Pass();
            mockRepository.VerifyAll();
        }
    }
}
