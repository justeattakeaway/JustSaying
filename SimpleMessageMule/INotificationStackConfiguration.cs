namespace SimpleMessageMule
{
    public interface INotificationStackConfiguration
    {
        int PublishFailureReAttempts { get; set; }
        int PublishFailureBackoffMilliseconds { get; set; }
        string Region { get; set; }
    }
}