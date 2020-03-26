using System.Collections.Generic;

namespace JustSaying.Messaging.Channels.Interrogation
{
    public class SubscriptionGroupsInterrogationResult
    {
        public SubscriptionGroupsInterrogationResult(IEnumerable<SubscriptionGroupInterrogationResult> groups)
        {
            Groups = groups;
        }

        public IEnumerable<SubscriptionGroupInterrogationResult> Groups { get; }
    }
}
