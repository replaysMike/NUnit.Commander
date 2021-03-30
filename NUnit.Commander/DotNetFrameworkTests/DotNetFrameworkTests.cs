using NUnit.Framework;

namespace DotNetFrameworkTests
{
    [TestFixture]
    public class DotNetFrameworkTests
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
        [Category("ExcludeFakeCategory1")]
        public void Should_Framework_Fail()
        {
            Assert.Fail("Example .net framework failure");
        }
    }
}