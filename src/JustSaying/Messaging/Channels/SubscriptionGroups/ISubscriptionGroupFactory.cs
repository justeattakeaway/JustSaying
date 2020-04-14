using System.Collections.Generic;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public interface ISubscriptionGroupFactory
    {
        SubscriptionGroupCollection Create(
            SubscriptionConfigBuilder defaults,
            IDictionary<string, SubscriptionGroupConfigBuilder> consumerGroupSettings);
    }
}
