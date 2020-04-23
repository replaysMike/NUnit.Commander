using Moq;
using NUnit.Commander.Display;
using NUnit.Framework;
using System;
using System.Drawing;

namespace NUnit.Commander.Tests.Display
{
    [TestFixture]
    public class ColorManagerTests
    {
        private MockRepository mockRepository;



        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);


        }

        private ColorScheme CreateManager()
        {
            return new ColorScheme(NUnit.Commander.Configuration.ColorSchemes.Default);
        }

        [Test]
        public void GetMappedConsoleColor_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var manager = CreateManager();
            var color = Color.Red;

            // Act
            var result = manager.GetMappedConsoleColor(color);

            // Assert
            Assert.AreEqual(ConsoleColor.Red, result);
            mockRepository.VerifyAll();
        }

        [Test]
        public void GetMappedColor_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var manager = CreateManager();
            var consoleColor = ConsoleColor.Red;

            // Act
            var result = manager.GetMappedColor(consoleColor);

            // Assert
            Assert.AreEqual(Color.Red, result);
            mockRepository.VerifyAll();
        }

        [Test]
        public void PrintColorsToConsole_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            manager.PrintColorsToConsole();

            // Assert
            Assert.Pass();
            mockRepository.VerifyAll();
        }
    }
}
