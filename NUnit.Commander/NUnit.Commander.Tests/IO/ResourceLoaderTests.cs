using Moq;
using NUnit.Commander.IO;
using NUnit.Framework;

namespace NUnit.Commander.Tests.IO
{
    [TestFixture]
    public class ResourceLoaderTests
    {
        private MockRepository mockRepository;



        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);


        }

        [Test]
        public void Load_LoadsResource_ExpectedBehavior()
        {
            // Arrange
            string name = "big.flf";

            // Act
            var result = ResourceLoader.Load(name);

            // Assert
            Assert.NotNull(result);
            Assert.Greater(result.Length, 0);
            mockRepository.VerifyAll();
            result.Dispose();
        }
    }
}
