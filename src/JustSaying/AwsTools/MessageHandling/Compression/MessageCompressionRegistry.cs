namespace JustSaying.AwsTools.MessageHandling.Compression;

public class MessageCompressionRegistry : IMessageCompressionRegistry
{
    private readonly IList<IMessageBodyCompression> _compressions;

    public MessageCompressionRegistry(IList<IMessageBodyCompression> compressions)
    {
        _compressions = compressions;
    }

    public IMessageBodyCompression GetCompression(string contentEncoding)
    {
        return _compressions.FirstOrDefault(x => x.ContentEncoding == contentEncoding);
    }
}
