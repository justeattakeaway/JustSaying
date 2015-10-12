using JustSaying.AwsTools;

namespace JustSaying
{
    public class PublishConfig : IPublishConfiguration
    {
        public PublishConfig()
        {

            PublishFailureReAttempts = JustSayingConstants.DEFAULT_PUBLISHER_RETRY_COUNT;
            PublishFailureBackoffMilliseconds = JustSayingConstants.DEFAULT_PUBLISHER_RETRY_INTERVAL;
        }
        public int PublishFailureReAttempts { get; set; }
        public int PublishFailureBackoffMilliseconds { get; set; }
    }
}