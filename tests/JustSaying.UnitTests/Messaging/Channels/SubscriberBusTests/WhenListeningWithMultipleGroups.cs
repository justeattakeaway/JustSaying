using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Configuration;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriberBusTests
{
    public class WhenListeningWithMultipleGroups : BaseSubscriptionBusTests
    {
        private readonly ISqsQueue _queueB;
        private readonly ISqsQueue _queueA;

        public WhenListeningWithMultipleGroups(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _queueA = CreateSuccessfulTestQueue(new TestMessage());
            _queueB = CreateSuccessfulTestQueue(new TestMessage());

        }

        protected override Dictionary<string, SubscriptionGroupSettingsBuilder> SetupBusConfig(SubscriptionConfig config)
        {
            return new Dictionary<string, SubscriptionGroupSettingsBuilder>
            {
                { "queueA", new SubscriptionGroupSettingsBuilder(config).AddQueue(_queueA) },
                { "queueB", new SubscriptionGroupSettingsBuilder(config).AddQueue(_queueB) }
            };
        }

        protected override void Given()
        {
            Queues.Add(_queueA);
            Queues.Add(_queueB);
        }
    }
}
