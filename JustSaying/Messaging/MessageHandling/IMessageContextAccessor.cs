namespace JustSaying.Messaging.MessageHandling
{
    public interface IMessageContextAccessor
    {
        MessageContext MessageContext { get; set; }
    }
}
