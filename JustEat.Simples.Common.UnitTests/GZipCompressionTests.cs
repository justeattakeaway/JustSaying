using System;
using System.Text;
using NUnit.Framework;

namespace JustEat.Simples.Common.UnitTests
{
    [TestFixture]
    class GZipCompressionTests
    {
        [Test]
        public void Compress_NullInput_Throws()
        {
            byte[] input = null;

            Assert.Throws<ArgumentNullException>(() => GZipCompression.Compress(input));
        }
        [Test]
        public void Decompress_NullInput_Throws()
        {
            byte[] input = null;

            Assert.Throws<ArgumentNullException>(() => GZipCompression.Decompress(input));
        }

        [Test]
        public void CompressThenDecompress_PassingInput_TheResultIsTheSameAsInput()
        {
            string anyText = "any_text";
            byte[] input = Encoding.UTF8.GetBytes(anyText);

            var compressed = GZipCompression.Compress(input);
            var decompressed = GZipCompression.Decompress(compressed);

            Assert.AreEqual(input, decompressed);
        }
    }
}
