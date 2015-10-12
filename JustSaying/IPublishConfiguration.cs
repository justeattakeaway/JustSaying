namespace JustSaying
{
    public interface IPublishConfiguration
    {
        int PublishFailureReAttempts { get; set; }
        int PublishFailureBackoffMilliseconds { get; set; }
        string Topic { get; set; }
    }
}