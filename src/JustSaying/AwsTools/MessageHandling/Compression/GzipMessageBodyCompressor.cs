using System.IO.Compression;
using System.Text;

namespace JustSaying.AwsTools.MessageHandling.Compression;

class GzipMessageBodyCompressor : IMessageBodyCompressor
{
    public string ContentEncoding { get; } = "gzip,base64";
    public string Compress(string messageBody)
    {
        var contentBytes = Encoding.UTF8.GetBytes(messageBody);
        using var compressedStream = new MemoryStream();
        using (var gZipStream = new GZipStream(compressedStream, CompressionMode.Compress))
        {
            gZipStream.Write(contentBytes, 0, contentBytes.Length);
        }

        return Convert.ToBase64String(compressedStream.ToArray());
    }
}
