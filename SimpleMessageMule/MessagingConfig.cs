using JustEat.Simples.NotificationStack.AwsTools;

namespace SimpleMessageMule
{
    public interface IMessagingConfig
    {
        int PublishFailureReAttempts { get; }
        int PublishFailureBackoffMilliseconds { get; }
        string Region { get; set; }

        void Validate();
    }
    public class MessagingConfig : INotificationStackConfiguration, IMessagingConfig
    {
        public MessagingConfig()
        {
            PublishFailureReAttempts = NotificationStackConstants.DEFAULT_PUBLISHER_RETRY_COUNT;
            PublishFailureBackoffMilliseconds = NotificationStackConstants.DEFAULT_PUBLISHER_RETRY_INTERVAL;
        }

        public int PublishFailureReAttempts { get; set; }
        public int PublishFailureBackoffMilliseconds { get; set; }
        public string Region { get; set; }

        public virtual void Validate()
        {
            //ToDo: Impl.
            //if (string.IsNullOrWhiteSpace(config.Region))
            //{
            //    config.Region = RegionEndpoint.EUWest1.SystemName;
            //    Log.Info("No Region was specified, using {0} by default.", config.Region);
            //}
            //throw new System.NotImplementedException();
        }
    }
}