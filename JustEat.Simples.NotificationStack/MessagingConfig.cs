using JustEat.Simples.NotificationStack.Messaging;

namespace JustEat.Simples.NotificationStack.Stack
{
    public class MessagingConfig : IMessagingConfig, INotificationStackConfiguration
    {
        public string Component { get; set; }
        public string Tenant { get; set; }
        public string Environment { get; set; }
        public int PublishFailureReAttempts { get; set; }
        public int PublishFailureBackoffMilliseconds { get; set; }
        public string Region { get; set; }
    }
}