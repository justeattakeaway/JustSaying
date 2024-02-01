using System.Runtime.Serialization;

namespace JustSaying.AwsTools.MessageHandling;

[Serializable]
public class PublishBatchException : PublishException
{
    public PublishBatchException()
        : base("Failed to publish batch of messages")
    {
    }

    public PublishBatchException(string message) : base(message)
    {
    }

    public PublishBatchException(string message, Exception inner) : base(message, inner)
    {
    }

    protected PublishBatchException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
