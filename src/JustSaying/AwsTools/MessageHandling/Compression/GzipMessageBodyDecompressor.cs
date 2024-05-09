using System.IO.Compression;
using System.Text;

namespace JustSaying.AwsTools.MessageHandling.Compression;

class GzipMessageBodyDecompressor : IMessageBodyDecompressor
{
    public string ContentEncoding { get; } = "gzip,base64";

    public string Decompress(string messageBody)
    {
        var compressedBytes = Convert.FromBase64String(messageBody);
        using var inputStream = new MemoryStream(compressedBytes);
        using var outputStream = new MemoryStream();
        using (var gZipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            gZipStream.CopyTo(outputStream);
        }

        return Encoding.UTF8.GetString(outputStream.ToArray());
    }
}
