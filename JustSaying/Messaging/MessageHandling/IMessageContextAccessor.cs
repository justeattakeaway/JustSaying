namespace JustSaying.Messaging.MessageHandling
{
    public interface IMessageContextReader
    {
        MessageContext MessageContext { get; }
    }

    public interface IMessageContextAccessor
    {
        MessageContext MessageContext { get; set; }
    }
}
