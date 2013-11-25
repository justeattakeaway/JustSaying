namespace JustEat.Simples.NotificationStack.Messaging
{
    public interface IMessagingConfig
    {
        string Component { get; }
        string Tenant { get; }
        string Environment { get; }
        int PublishFailureReAttempts { get; }
        int PublishFailureBackoffMilliseconds { get; }
        string Region { get; set; }
    }
}