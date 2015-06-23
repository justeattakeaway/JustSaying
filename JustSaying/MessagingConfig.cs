using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools;
using JustSaying.Extensions;

namespace JustSaying
{
    public class MessagingConfig : IMessagingConfig
    {
        public MessagingConfig()
        {
            PublishFailureReAttempts = JustSayingConstants.DEFAULT_PUBLISHER_RETRY_COUNT;
            PublishFailureBackoffMilliseconds = JustSayingConstants.DEFAULT_PUBLISHER_RETRY_INTERVAL;
            TopicNameProvider = type => type.ToTopicName();
            Regions = new List<string>();
        }

        public int PublishFailureReAttempts { get; set; }
        public int PublishFailureBackoffMilliseconds { get; set; }
        public Func<Type, string> TopicNameProvider { get; set; }
        public IList<string> Regions { get; private set; }
        public Func<string> GetActiveRegion { get; set; }

        public virtual void Validate()
        {
            if (!Regions.Any() || string.IsNullOrWhiteSpace(Regions.First()))
            {
                throw new ArgumentNullException("config.Regions", "Cannot have a blank entry for config.Regions");
            }
        }
    }
}