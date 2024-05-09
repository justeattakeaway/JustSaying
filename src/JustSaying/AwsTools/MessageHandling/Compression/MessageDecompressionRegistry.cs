namespace JustSaying.AwsTools.MessageHandling.Compression;

public class MessageDecompressionRegistry : IMessageDecompressionRegistry
{
    private readonly IList<IMessageBodyDecompressor> _decompressors;

    public MessageDecompressionRegistry(IList<IMessageBodyDecompressor> decompressors)
    {
        _decompressors = decompressors;
    }

    public IMessageBodyDecompressor GetDecompressor(string contentEncoding)
    {
        return _decompressors.FirstOrDefault(x => x.ContentEncoding == contentEncoding);
    }
}
