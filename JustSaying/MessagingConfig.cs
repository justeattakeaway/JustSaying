using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools;

namespace JustSaying
{
    public interface IMessagingConfig //ToDo: This vs publish config. Clean it up. not good.
    {
        int PublishFailureReAttempts { get; }
        int PublishFailureBackoffMilliseconds { get; }
        IList<string> Regions { get; }

        void Validate();
    }

    public class MessagingConfig : IPublishConfiguration, IMessagingConfig
    {
        public MessagingConfig()
        {
            PublishFailureReAttempts = JustSayingConstants.DEFAULT_PUBLISHER_RETRY_COUNT;
            PublishFailureBackoffMilliseconds = JustSayingConstants.DEFAULT_PUBLISHER_RETRY_INTERVAL;
            Regions = new List<string>();
        }

        public int PublishFailureReAttempts { get; set; }
        public int PublishFailureBackoffMilliseconds { get; set; }
        public string Region { get; set; }
        public IList<string> Regions { get; set; }

        public virtual void Validate()
        {
            if (!Regions.Any() || string.IsNullOrWhiteSpace(Regions.First()))
            {
                throw new ArgumentNullException("config.Regions", "Cannot have a blank entry for config.Regions");
            }
        }
    }
}