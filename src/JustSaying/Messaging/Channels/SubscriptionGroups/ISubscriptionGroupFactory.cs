using System.Collections.Generic;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    internal interface ISubscriptionGroupFactory
    {
        SubscriptionGroupCollection Create(
            IDictionary<string, SubscriptionGroupSettingsBuilder> consumerGroupSettings);
    }
}
