namespace JustSaying.Messaging.MessageSerialization;

internal sealed class SqsMessageEnvelope
{
    public string Subject { get; set; }

    public string Message { get; set; }
}
