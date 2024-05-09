namespace JustSaying.AwsTools.MessageHandling.Compression;

public interface IMessageBodyDecompressor
{
    string ContentEncoding { get; }
    string Decompress(string messageBody);
}
