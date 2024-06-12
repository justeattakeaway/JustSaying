using System.IO.Compression;
using System.Text;

namespace JustSaying.AwsTools.MessageHandling.Compression;

public class GzipMessageBodyCompression : IMessageBodyCompression
{
    public string ContentEncoding => "gzip,base64";

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
