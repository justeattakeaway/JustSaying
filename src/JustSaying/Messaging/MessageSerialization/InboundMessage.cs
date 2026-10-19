using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Represents a deserialized message with attributes.
/// </summary>
public sealed class InboundMessage(object message, MessageAttributes messageAttributes)
{
    /// <summary>
    /// Gets the message that was extracted from a message body.
    /// </summary>
    public object Message { get; } = message;

    /// <summary>
    /// Gets the attributes that were extracted from a message body.
    /// </summary>
    public MessageAttributes MessageAttributes { get; } = messageAttributes;

    public void Deconstruct(out object message, out MessageAttributes attributes)
    {
        message = Message;
        attributes = MessageAttributes;
    }
}
