using System.Runtime.Serialization;

namespace JustSaying.AwsTools.MessageHandling;

[Serializable]
public class PublishException : Exception
{
    public PublishException() : base("Failed to publish message")
    {
    }

    public PublishException(string message) : base(message)
    {
    }

    public PublishException(string message, Exception inner) : base(message, inner)
    {
    }

#if !NET8_0_OR_GREATER
    protected PublishException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
#endif
}
