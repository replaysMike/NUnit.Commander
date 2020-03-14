using Moq;
using NUnit.Commander.Configuration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections;

namespace NUnit.Commander.Tests.Configuration
{
    [TestFixture]
    public class ConfigurationProviderTests
    {
        private MockRepository mockRepository;

        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);
        }

        private ConfigurationProvider CreateProvider()
        {
            return new ConfigurationProvider();
        }

        [Test]
        public void LoadConfiguration_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var config = provider.LoadConfiguration();

            // Assert
            Assert.NotNull(config);
            mockRepository.VerifyAll();
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var provider = CreateProvider();
            var config = provider.LoadConfiguration();

            // Act
            var result = provider.Get<ApplicationConfiguration>(config);

            // Assert
            Assert.NotNull(result);
            mockRepository.VerifyAll();
        }
    }
}
