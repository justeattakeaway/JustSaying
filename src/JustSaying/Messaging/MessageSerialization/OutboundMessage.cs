namespace JustSaying.Messaging.MessageSerialization;

public sealed class OutboundMessage
{
    public OutboundMessage(string body, Dictionary<string, MessageAttributeValue> messageAttributes, string subject)
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
