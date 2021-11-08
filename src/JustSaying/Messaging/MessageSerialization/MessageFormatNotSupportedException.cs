using System.Runtime.Serialization;

namespace JustSaying.Messaging.MessageSerialization
{
    [Serializable]
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

        protected MessageFormatNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
