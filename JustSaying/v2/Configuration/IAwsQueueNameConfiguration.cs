namespace JustSaying.v2.Configuration
{
    public interface IAwsQueueNameConfiguration
    {
        string QueueNameOverride { get; set; }
    }
}