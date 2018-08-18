using System.Linq;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenSettingUpMultipleHandlers : XBehaviourTest<IHaveFulfilledSubscriptionRequirements>
    {
        private ProxyAwsClientFactory _proxyAwsClientFactory;
        private string _topicName;
        private string _queueName;

        protected override void Given()
        {
        }

        protected override void When()
        {
        }

        protected override IHaveFulfilledSubscriptionRequirements CreateSystemUnderTest()
        {
            // Given 2 handlers
            var uniqueTopicAndQueueNames = new UniqueNamingStrategy();
            _proxyAwsClientFactory = new ProxyAwsClientFactory();

            var baseQueueName = "CustomerOrders_";
            _topicName = uniqueTopicAndQueueNames.GetTopicName(string.Empty, typeof(Order));
            _queueName = uniqueTopicAndQueueNames.GetQueueName(new SqsReadConfiguration(SubscriptionType.ToTopic) { BaseQueueName = baseQueueName }, typeof(Order));

            var bus = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(TestEnvironment.Region.SystemName)
                .WithAwsClientFactory(() => _proxyAwsClientFactory)
                .WithNamingStrategy(() => uniqueTopicAndQueueNames)
                .WithSqsTopicSubscriber()
                .IntoQueue(baseQueueName)
                .WithMessageHandlers(new OrderHandler(), new OrderHandler());

            bus.StartListening();
            return bus;
        }

        protected override void PostAssertTeardown()
        {
            SystemUnderTest.StopListening();
            base.PostAssertTeardown();
        }

        [Fact]
        public void CreateTopicCalled()
        {
            _proxyAwsClientFactory.Counters["CreateTopic"][_topicName].Count.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public void GetQueueAttributesCalledOnce()
        {
            _proxyAwsClientFactory.Counters["GetQueueAttributes"].First(x => x.Key.EndsWith(_queueName)).Value.Count
                .ShouldBe(1);
        }

        [Fact]
        public void CreateQueueCalledOnce()
        {
            AssertHasCounterSetToOne("CreateQueue", _queueName);
        }

        private void AssertHasCounterSetToOne(string counter, string testQueueName)
        {
            var counters = _proxyAwsClientFactory.Counters;

            counters.ShouldContainKey(counter, $"no counter: {counter}");
            counters[counter].ShouldContainKey(testQueueName, $"no queueName: {testQueueName}");
            counters[counter][testQueueName].Count.ShouldBe(1, "Wrong count");
        }
    }
}
