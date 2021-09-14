using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization
{
    /// <summary>
    /// Represents a deserialized message with attributes.
    /// </summary>
    public class MessageWithAttributes
    {
        public MessageWithAttributes(Message message, MessageAttributes messageAttributes)
        {
            Message = message;
            MessageAttributes = messageAttributes;
        }

        /// <summary>
        /// Gets the message that was extracted from a message body.
        /// </summary>
        public Message Message { get; }

        /// <summary>
        /// Gets the attributes that were extracted from a message body.
        /// </summary>
        public MessageAttributes MessageAttributes { get; }
    }
}
