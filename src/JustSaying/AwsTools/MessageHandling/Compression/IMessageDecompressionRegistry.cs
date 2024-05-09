namespace JustSaying.AwsTools.MessageHandling.Compression;

public interface IMessageDecompressionRegistry
{
    IMessageBodyDecompressor GetDecompressor(string contentEncoding);
}
