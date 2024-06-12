namespace JustSaying.AwsTools.MessageHandling.Compression;

public interface IMessageBodyCompression
{
    string ContentEncoding { get; }
    string Compress(string messageBody);
    string Decompress(string messageBody);
}
