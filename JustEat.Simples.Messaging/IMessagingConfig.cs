namespace JustEat.Simples.NotificationStack.Messaging
{
    public interface IMessagingConfig
    {
        string Tenant { get; }
        string Environment { get; }
    }
}