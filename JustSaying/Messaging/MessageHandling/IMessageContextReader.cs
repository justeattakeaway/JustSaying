namespace JustSaying.Messaging.MessageHandling
{
    public interface IMessageContextReader
    {
        MessageContext MessageContext { get; }
    }
}
