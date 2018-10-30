using System;
using System.Runtime.Serialization;

namespace JustSaying.Messaging.MessageSerialisation
{
    [Serializable]
    public class MessageFormatNotSupportedException : Exception
    {
        public MessageFormatNotSupportedException() : base("message format not supported")
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
