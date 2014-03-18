namespace JustEat.Simples.NotificationStack.Messaging
{
    public interface IBaseMessagingConfig
    {
        int PublishFailureReAttempts { get; }
        int PublishFailureBackoffMilliseconds { get; }
        string Region { get; set; }
    }
    public interface IMessagingConfig : IBaseMessagingConfig
    {
        string Component { get; }
        string Tenant { get; }
        string Environment { get; }
    }
}