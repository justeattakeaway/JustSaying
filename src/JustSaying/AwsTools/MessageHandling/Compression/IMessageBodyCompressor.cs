namespace JustSaying.AwsTools.MessageHandling.Compression;

internal interface IMessageBodyCompressor
{
    string ContentEncoding { get; }
    string Compress(string messageBody);
}
