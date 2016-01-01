using System;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class MessageFormatNotSupportedException : Exception
    {
        public MessageFormatNotSupportedException(string message) : base(message)
        {
        }
    }
}