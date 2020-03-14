using Moq;
using NUnit.Commander.Display;
using NUnit.Framework;
using System.Drawing;

namespace NUnit.Commander.Tests.Display
{
    [TestFixture]
    public class DisplayUtilTests
    {
        private MockRepository mockRepository;



        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);


        }

        [Test]
        public void GetPrettyTestName_TestName_ExpectedBehavior()
        {
            // Arrange
            string fullName = "TestingPath.TestingTestName(Arguments here)";
            var pathColor = Color.Red;
            var testNameColor = Color.White;
            var argsColor = Color.Yellow;
            int maxTestCaseArgumentLength = 0;

            // Act
            var result = DisplayUtil.GetPrettyTestName(
                fullName,
                pathColor,
                testNameColor,
                argsColor,
                maxTestCaseArgumentLength);

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(fullName.Length, result.Length);
            Assert.AreEqual("TestingPath.", result.TextFragments[0].Text);
            Assert.AreEqual(pathColor, result.TextFragments[0].ForegroundColor);
            Assert.AreEqual("TestingTestName", result.TextFragments[1].Text);
            Assert.AreEqual(testNameColor, result.TextFragments[1].ForegroundColor);
            Assert.AreEqual("(Arguments here)", result.TextFragments[2].Text);
            Assert.AreEqual(argsColor, result.TextFragments[2].ForegroundColor);
            mockRepository.VerifyAll();
        }

        [Test]
        public void GetPrettyTestName_LongTestName_ExpectedBehavior()
        {
            // Arrange
            string fullName = "TestingPathSuperLongPathThatIsReallyLongAndShouldGetTruncated.TestingTestNameWithAReallyLongPath(Arguments here and more here because this is too long)";
            var pathColor = Color.Red;
            var testNameColor = Color.White;
            var argsColor = Color.Yellow;

            // Act
            DisplayUtil.MaxWidth = 94;
            var result = DisplayUtil.GetPrettyTestName(
                fullName,
                pathColor,
                testNameColor,
                argsColor
                );

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(3, result.TextFragments.Count);
            Assert.AreEqual("Te...", result.TextFragments[0].Text);
            Assert.AreEqual(pathColor, result.TextFragments[0].ForegroundColor);
            Assert.AreEqual("TestingTestNameWithAReallyLongPath", result.TextFragments[1].Text);
            Assert.AreEqual(testNameColor, result.TextFragments[1].ForegroundColor);
            Assert.AreEqual("(Arguments here and more here because this is too long)", result.TextFragments[2].Text);
            Assert.AreEqual(argsColor, result.TextFragments[2].ForegroundColor);
            mockRepository.VerifyAll();
        }

        [Test]
        public void GetFriendlyBytes_ParsesB_ExpectedBehavior()
        {
            // Arrange
            var value = 4;
            int decimalPlaces = 1;

            // Act
            var result = DisplayUtil.GetFriendlyBytes(value, decimalPlaces);

            // Assert
            Assert.AreEqual("4.0 bytes", result);
            mockRepository.VerifyAll();
        }

        [Test]
        public void GetFriendlyBytes_ParsesKB_ExpectedBehavior()
        {
            // Arrange
            var value = 4 * 1024L;
            int decimalPlaces = 1;

            // Act
            var result = DisplayUtil.GetFriendlyBytes(value, decimalPlaces);

            // Assert
            Assert.AreEqual("4.0 KB", result);
            mockRepository.VerifyAll();
        }

        [Test]
        public void GetFriendlyBytes_ParsesMB_ExpectedBehavior()
        {
            // Arrange
            var value = 4 * 1024L * 1024L;
            int decimalPlaces = 1;

            // Act
            var result = DisplayUtil.GetFriendlyBytes(value, decimalPlaces);

            // Assert
            Assert.AreEqual("4.0 MB", result);
            mockRepository.VerifyAll();
        }

        [Test]
        public void GetFriendlyBytes_ParsesGB_ExpectedBehavior()
        {
            // Arrange
            var value = 4 * 1024L * 1024L * 1024L;
            int decimalPlaces = 1;

            // Act
            var result = DisplayUtil.GetFriendlyBytes(value, decimalPlaces);

            // Assert
            Assert.AreEqual("4.0 GB", result);
            mockRepository.VerifyAll();
        }
    }
}
