using JustSaying.Messaging.Channels.SubscriptionGroups;

namespace JustSaying.Messaging.Channels.Interrogation
{
    public class SubscriptionGroupInterrogationResult
    {
        public SubscriptionGroupInterrogationResult(SubscriptionGroupSettings settings)
        {
            Settings = settings;
        }

        public SubscriptionGroupSettings Settings { get; }
    }
}
