using System;
using System.IO;
using System.IO.Compression;

namespace JustEat.Simples.Common
{
    public static class GZipCompression
    {
        public static byte[] Compress(byte[] input)
        {
            if(input == null)
                throw new ArgumentNullException("input");

            using (var mStream = new MemoryStream(input))
            {
                using (var outStream = new MemoryStream())
                {
                    using (var tinyStream = new GZipStream(outStream, CompressionMode.Compress))
                    {
                        mStream.CopyTo(tinyStream);
                    }
                    var compressed = outStream.ToArray();
                    return compressed;
                }
            }
        }

        public static byte[] Decompress(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            using (var bigStream = new GZipStream(new MemoryStream(input), CompressionMode.Decompress))
            {
                using (var bigStreamOut = new MemoryStream())
                {
                    bigStream.CopyTo(bigStreamOut);
                    return bigStreamOut.ToArray();
                }
            }
        }
    }
}
