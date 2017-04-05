using System;
using System.Threading.Tasks;
using Amazon;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    [TestFixture]
    public class WhenSettingUpMultipleHandlers : BehaviourTest<IHaveFulfilledSubscriptionRequirements>
    {
        public class Order : Models.Message
        {
        }

        public class OrderHandler : IHandlerAsync<Order>
        {
            public Task<bool> Handle(Order message)
            {
                return Task.FromResult(true);
            }
        }

        public class UniqueTopicAndQueueNames : INamingStrategy
        {
            private readonly long ticks = DateTime.UtcNow.Ticks;

            public string GetTopicName(string topicName, string messageType)
            {
                return (messageType + ticks).ToLower();
            }

            public string GetQueueName(SqsReadConfiguration sqsConfig, string messageType)
            {
                return (sqsConfig.BaseQueueName + ticks).ToLower();
            }
        }

        protected string QueueUniqueKey;
        private UniqueTopicAndQueueNames uniqueTopicAndQueueNames;
        private ProxyAwsClientFactory proxyAwsClientFactory;
        IHaveFulfilledSubscriptionRequirements bus;
        private string topicName;
        private string queueName;

        protected override void Given()
        { }

        protected override IHaveFulfilledSubscriptionRequirements CreateSystemUnderTest()
        {
            // Given 2 handlers
            uniqueTopicAndQueueNames = new UniqueTopicAndQueueNames();
            proxyAwsClientFactory = new ProxyAwsClientFactory();

            var baseQueueName = "CustomerOrders_";
            topicName = uniqueTopicAndQueueNames.GetTopicName(string.Empty, typeof(Order).Name);
            queueName = uniqueTopicAndQueueNames.GetQueueName(new SqsReadConfiguration(SubscriptionType.ToTopic) {BaseQueueName = baseQueueName }, typeof(Order).Name);

            bus = CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithAwsClientFactory(() => proxyAwsClientFactory)
                .WithNamingStrategy(() => uniqueTopicAndQueueNames)
                .WithSqsTopicSubscriber()
                .IntoQueue(baseQueueName) // generate unique queue name
                .WithMessageHandlers(new OrderHandler(), new OrderHandler());

            bus.StartListening();
            return bus;
        }
        public override void PostAssertTeardown()
        {
            SystemUnderTest.StopListening();
            base.PostAssertTeardown();
        }

        protected override void When()
        {
        }

        [Test]
        public void CreateTopicCalledOnce()
        {
            AssertHasCounterSetToOne("CreateTopic", topicName);
        }

        [Test]
        public void FindTopicCalledOnce()
        {
            AssertHasCounterSetToOne("FindTopic", topicName);
        }

        [Test]
        public void ListQueuesCalledOnce()
        {
            AssertHasCounterSetToOne("ListQueues", queueName);
        }

        [Test]
        public void CreateQueueCalledOnce()
        {
            AssertHasCounterSetToOne("CreateQueue", queueName);
        }

        private void AssertHasCounterSetToOne(string counter, string testQueueName)
        {
            var counters = proxyAwsClientFactory.Counters;

            Assert.That(counters.ContainsKey(counter), Is.True, "no counter: " + counter);
            Assert.That(counters[counter].ContainsKey(testQueueName), Is.True, "no queueName: " + testQueueName);
            Assert.That(counters[counter][testQueueName].Count, Is.EqualTo(1), "Wrong count");
        }
    }
}
