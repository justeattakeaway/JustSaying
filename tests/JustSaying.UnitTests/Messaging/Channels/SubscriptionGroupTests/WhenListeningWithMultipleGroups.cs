using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenListeningWithMultipleGroups : BaseSubscriptionGroupTests
    {
        private readonly ISqsQueue _queueB;
        private readonly ISqsQueue _queueA;

        public WhenListeningWithMultipleGroups(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _queueA = CreateSuccessfulTestQueue("TestQueueA", new TestMessage());
            _queueB = CreateSuccessfulTestQueue("TestQueueB", new TestMessage());
        }

        protected override Dictionary<string, SubscriptionGroupConfigBuilder> SetupBusConfig()
        {
            return new Dictionary<string, SubscriptionGroupConfigBuilder>
            {
                {
                    "queueA", new SubscriptionGroupConfigBuilder("queueA")
                        .AddQueue(_queueA)
                        .WithPrefetch(5)
                        .WithBufferSize(20)
                        .WithConcurrencyLimit(1)
                        .WithMultiplexerCapacity(30)
                },
                { "queueB", new SubscriptionGroupConfigBuilder("queueB").AddQueue(_queueB) }
            };
        }

        protected override void Given()
        {
            Queues.Add(_queueA);
            Queues.Add(_queueB);
        }

        [Fact]
        public void SubscriptionGroups_OverridesDefaultSettingsCorrectly()
        {
            var interrogationResult = SystemUnderTest.Interrogate();

            var json = JsonConvert.SerializeObject(interrogationResult, Formatting.Indented);

            json.ShouldMatchApproved(c => c.SubFolder("Approvals"));
        }
    }
}
