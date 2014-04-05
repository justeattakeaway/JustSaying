namespace JustSaying
{
    public interface IPublishConfiguration
    {
        int PublishFailureReAttempts { get; set; }
        int PublishFailureBackoffMilliseconds { get; set; }
        string Region { get; set; }
    }
}