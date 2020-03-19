using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Channels.Configuration;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using JustSaying.Naming;

namespace JustSaying
{
    public class MessagingConfig : IMessagingConfig
    {
        public MessagingConfig()
        {
            PublishFailureReAttempts = JustSayingConstants.DefaultPublisherRetryCount;
            PublishFailureBackoff = JustSayingConstants.DefaultPublisherRetryInterval;
            AdditionalSubscriberAccounts = new List<string>();
            Regions = new List<string>();
            MessageSubjectProvider = new NonGenericMessageSubjectProvider();
            TopicNamingConvention = new DefaultNamingConventions();
            QueueNamingConvention = new DefaultNamingConventions();
            ConsumerGroupConfig = new ConsumerGroupConfig();
        }

        public int PublishFailureReAttempts { get; set; }
        public TimeSpan PublishFailureBackoff { get; set; }
        public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
        public IReadOnlyCollection<string> AdditionalSubscriberAccounts { get; set; }
        public IList<string> Regions { get; }
        public Func<string> GetActiveRegion { get; set; }
        public IMessageSubjectProvider MessageSubjectProvider { get; set; }
        public ITopicNamingConvention TopicNamingConvention { get; set; }
        public IQueueNamingConvention QueueNamingConvention { get; set; }
        public ConsumerGroupConfig ConsumerGroupConfig { get; set; }

        public virtual void Validate()
        {
            if (!Regions.Any())
            {
                throw new InvalidOperationException($"Config needs values for the {nameof(Regions)} property.");
            }

            if (Regions.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(Regions)} property.");
            }

            var duplicateRegions = Regions
                .GroupBy(x => x)
                .Where(y => y.Count() > 1)
                .Select(r => r.Key)
                .ToList();

            if (duplicateRegions.Count > 0)
            {
                var regionsText = string.Join(",", duplicateRegions);
                throw new InvalidOperationException($"Config has duplicates in {nameof(Regions)} for '{regionsText}'.");
            }

            if (MessageSubjectProvider == null)
            {
                throw new InvalidOperationException($"Config cannot have a null for the {nameof(MessageSubjectProvider)} property.");
            }

            // Todo: validate the consumer config
        }
    }
}
