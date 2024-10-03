using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Represents a deserialized message with attributes.
/// </summary>
public sealed class ReceivedMessage(Message message, MessageAttributes messageAttributes)
{
    /// <summary>
    /// Gets the message that was extracted from a message body.
    /// </summary>
    public Message Message { get; } = message;

    /// <summary>
    /// Gets the attributes that were extracted from a message body.
    /// </summary>
    public MessageAttributes MessageAttributes { get; } = messageAttributes;

    public void Deconstruct(out Message message, out MessageAttributes attributes)
    {
        message = Message;
        attributes = MessageAttributes;
    }
}

public sealed class PublishMessage
{
    public PublishMessage(string body, Dictionary<string, MessageAttributeValue> messageAttributes, string subject)
    {
        Body = body;
        MessageAttributes = messageAttributes;
        Subject = subject;
    }

    public string Body { get; }
    public Dictionary<string, MessageAttributeValue> MessageAttributes { get; }
    public string Subject { get; }

    public void Deconstruct(out string body, out Dictionary<string, MessageAttributeValue> attributes, out string subject)
    {
        body = Body;
        attributes = MessageAttributes;
        subject = Subject;
    }
}
