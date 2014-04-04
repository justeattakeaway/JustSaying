using System;
using JustSaying.AwsTools;

namespace JustSaying
{
    public interface IMessagingConfig //ToDo: This vs publish config. Clean it up. not good.
    {
        int PublishFailureReAttempts { get; }
        int PublishFailureBackoffMilliseconds { get; }
        string Region { get; set; }

        void Validate();
    }

    public class MessagingConfig : IPublishConfiguration, IMessagingConfig
    {
        public MessagingConfig()
        {
            PublishFailureReAttempts = JustSayingConstants.DEFAULT_PUBLISHER_RETRY_COUNT;
            PublishFailureBackoffMilliseconds = JustSayingConstants.DEFAULT_PUBLISHER_RETRY_INTERVAL;
        }

        public int PublishFailureReAttempts { get; set; }
        public int PublishFailureBackoffMilliseconds { get; set; }
        public string Region { get; set; }

        public virtual void Validate()
        {
            if (string.IsNullOrWhiteSpace(Region))
                throw new ArgumentNullException("config.Region", "Cannot have a blank entry for config.Region");
        }
    }
}