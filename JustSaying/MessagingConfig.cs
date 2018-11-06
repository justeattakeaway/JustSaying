using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;

namespace JustSaying
{
    public class MessagingConfig : IMessagingConfig
    {
        public MessagingConfig()
        {
            PublishFailureReAttempts = JustSayingConstants.DefaultPublisherRetryCount;
            PublishFailureBackoffMilliseconds = JustSayingConstants.DefaultPublisherRetryInterval;
            AdditionalSubscriberAccounts = new List<string>();
            Regions = new List<string>();
            MessageSubjectProvider = new NonGenericMessageSubjectProvider();
        }

        public int PublishFailureReAttempts { get; set; }
        public int PublishFailureBackoffMilliseconds { get; set; }
        public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
        public IReadOnlyCollection<string> AdditionalSubscriberAccounts { get; set; }
        public IList<string> Regions { get; }
        public Func<string> GetActiveRegion { get; set; }
        public IMessageSubjectProvider MessageSubjectProvider { get; set; }

        public virtual void Validate()
        {
            if (!Regions.Any())
            {
                throw new InvalidOperationException($"Config needs values for the {nameof(Regions)} property.");
            }

            if (string.IsNullOrWhiteSpace(Regions.First()))
            {
                throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(Regions)} property.");
            }

            var duplicateRegion = Regions
                .GroupBy(x => x)
                .FirstOrDefault(y => y.Count() > 1);

            if (duplicateRegion != null)
            {
                throw new InvalidOperationException($"Config has a duplicate in {nameof(Regions)} for '{duplicateRegion.Key}'.");
            }

            if (MessageSubjectProvider == null)
            {
                throw new InvalidOperationException($"Config cannot have a null for the {nameof(MessageSubjectProvider)} property.");
            }
        }
    }
}
