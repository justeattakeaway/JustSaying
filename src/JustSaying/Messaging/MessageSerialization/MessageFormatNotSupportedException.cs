#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace JustSaying.Messaging.MessageSerialization;

#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class MessageFormatNotSupportedException : Exception
{
    public MessageFormatNotSupportedException() : base("Message format not supported")
    {
    }

    public MessageFormatNotSupportedException(string message) : base(message)
    {
    }

    public MessageFormatNotSupportedException(string message, Exception innerException) : base(message, innerException)
    {
    }

#if !NET8_0_OR_GREATER
    protected MessageFormatNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
#endif
}
