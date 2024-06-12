namespace JustSaying.AwsTools.MessageHandling.Compression;

public interface IMessageCompressionRegistry
{
    IMessageBodyCompression GetCompression(string contentEncoding);
}
