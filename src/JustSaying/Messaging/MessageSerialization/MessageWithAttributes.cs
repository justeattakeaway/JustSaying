using Amazon.SQS.Model;
using Message = JustSaying.Models.Message;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Represents a deserialized message with attributes.
/// </summary>
public sealed class MessageWithAttributes
{
    public MessageWithAttributes(Message message, Dictionary<string, MessageAttributeValue> messageAttributes)
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
    public Dictionary<string, MessageAttributeValue> MessageAttributes { get; }

    /// <summary>
    /// Deconstructs the instance into message and attributes.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="attributes"></param>
    public void Deconstruct(out Message message, out Dictionary<string, MessageAttributeValue> attributes)
    {
        message = Message;
        attributes = MessageAttributes;
    }
}
