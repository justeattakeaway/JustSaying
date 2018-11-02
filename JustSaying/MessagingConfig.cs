using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public IList<string> Regions { get; private set; }
        public Func<string> GetActiveRegion { get; set; }
        public IMessageSubjectProvider MessageSubjectProvider { get; set; }

        public virtual void Validate()
        {
            if (!Regions.Any() || string.IsNullOrWhiteSpace(Regions.First()))
            {
                throw new ArgumentNullException("config.Regions", "Cannot have a blank entry for config.Regions");
            }

            var duplicateRegion = Regions.GroupBy(x => x).FirstOrDefault(y => y.Count() > 1);
            if (duplicateRegion != null)
            {
                throw new ArgumentException($"Region {duplicateRegion.Key} was added multiple times");
            }

            if (MessageSubjectProvider == null)
                throw new ArgumentNullException("config.MessageSubjectProvider");
        }
    }
}
