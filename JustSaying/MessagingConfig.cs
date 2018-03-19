using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying
{
    public class MessagingConfig : IMessagingConfig
    {
        public MessagingConfig()
        {
            PublishFailureReAttempts = JustSayingConstants.DEFAULT_PUBLISHER_RETRY_COUNT;
            PublishFailureBackoffMilliseconds = JustSayingConstants.DEFAULT_PUBLISHER_RETRY_INTERVAL;
            AdditionalSubscriberAccounts = new List<string>();
            Regions = new List<string>();
        }

        public int PublishFailureReAttempts { get; set; }
        public int PublishFailureBackoffMilliseconds { get; set; }
        public IMessageResponseLogger MessageResponseLogger { get; set; }
        public IReadOnlyCollection<string> AdditionalSubscriberAccounts { get; set; }
        public IList<string> Regions { get; private set; }
        public Func<string> GetActiveRegion { get; set; }

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
        }
    }
}
