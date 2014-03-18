using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging;

namespace JustEat.Simples.NotificationStack.Stack
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