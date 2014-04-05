using JustSaying.AwsTools;
using JustSaying.Messaging;

namespace JustSaying.Stack
{
    public class MessagingConfig : IMessagingConfig, INotificationStackConfiguration
    {
        public MessagingConfig()
        {
            PublishFailureReAttempts = NotificationStackConstants.DEFAULT_PUBLISHER_RETRY_COUNT;
            PublishFailureBackoffMilliseconds = NotificationStackConstants.DEFAULT_PUBLISHER_RETRY_INTERVAL;
        }
        public string Component { get; set; }
        public string Tenant { get; set; }
        public string Environment { get; set; }
        public int PublishFailureReAttempts { get; set; }
        public int PublishFailureBackoffMilliseconds { get; set; }
        public string Region { get; set; }
    }
}