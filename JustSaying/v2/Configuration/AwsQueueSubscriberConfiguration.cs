namespace JustSaying.v2.Configuration
{
    public interface IAwsQueueSubscriberConfiguration : IAwsSubscriberConfiguration
    {
    }

    public class AwsQueueSubscriberConfiguration : AwsSubscriberConfiguration, IAwsQueueSubscriberConfiguration
    {
    }
}