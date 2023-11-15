namespace JustSaying.Messaging.MessageHandling;

public class MessageContextAccessor : IMessageContextReader, IMessageContextAccessor
{
    private static readonly AsyncLocal<MessageContext> Context = new();

    public MessageContext MessageContext
    {
        get => Context.Value;
        set => Context.Value = value;
    }
}
