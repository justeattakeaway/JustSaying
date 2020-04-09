using System.Collections.Generic;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public interface ISubscriptionGroupFactory
    {
        SubscriptionGroupCollection Create(
            IDictionary<string, SubscriptionGroupSettingsBuilder> consumerGroupSettings);
    }
}
