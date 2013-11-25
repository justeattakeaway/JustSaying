namespace JustEat.Simples.NotificationStack.Stack
{
    public interface INotificationStackConfiguration
    {
        string Component { get; set; }
        string Tenant { get; set; }
        string Environment { get; set; }
        int PublishFailureReAttempts { get; set; }
        int PublishFailureBackoffMilliseconds { get; set; }
    }
}