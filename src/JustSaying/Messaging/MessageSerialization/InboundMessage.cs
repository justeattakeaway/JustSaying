using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Represents a deserialized message with attributes.
/// </summary>
public sealed class InboundMessage(object message, MessageAttributes messageAttributes, MessageContextFactory messageContextFactory = null)
{
    /// <summary>
    /// Gets the message that was extracted from a message body.
    /// </summary>
    public object Message { get; } = message;

    /// <summary>
    /// Gets the attributes that were extracted from a message body.
    /// </summary>
    public MessageAttributes MessageAttributes { get; } = messageAttributes;

    /// <summary>
    /// Gets the factory that creates the <see cref="MessageContext"/> for this message, or
    /// <see langword="null"/> to use the default context.
    /// </summary>
    public MessageContextFactory MessageContextFactory { get; } = messageContextFactory;

    public void Deconstruct(out object message, out MessageAttributes attributes)
    {
        message = Message;
        attributes = MessageAttributes;
    }
}
