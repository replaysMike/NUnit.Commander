using NUnit.Framework;

namespace DotNetCoreTests
{
    [TestFixture]
    public class DotNetCoreTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Should_Pass1()
        {
            Assert.Pass();
        }

        [Test]
        public void Should_Pass2()
        {
            Assert.Pass();
        }

        [Test]
        public void Should_Pass3()
        {
            Assert.Pass();
        }

        [Test]
        public void Should_Core_Fail()
        {
            Assert.Fail("Example .net core failure");
        }

        [Test]
        [Category("ExcludeFakeCategory1")]
        public void Should_Core_Fail2()
        {
            Assert.Fail("Example .net core failure 2");
        }
    }
}