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
    public class WhenSettingUpMultipleHandlers : BehaviourTest<IMessageBus>
    {
        private class Order : Models.Message
        {
        }

        private class OrderHandler : IHandlerAsync<Order>
        {
            public Task<bool> Handle(Order message) => Task.FromResult(true);
        }

        private class UniqueTopicAndQueueNames : INamingStrategy
        {
            private readonly long _ticks = DateTime.UtcNow.Ticks;

            public string GetTopicName(string topicName, string messageType) => (messageType + _ticks).ToLower();

            public string GetQueueName(SqsReadConfiguration sqsConfig, string messageType) => (sqsConfig.BaseQueueName + _ticks).ToLower();
        }
        
        private UniqueTopicAndQueueNames _uniqueTopicAndQueueNames;
        private ProxyAwsClientFactory _proxyAwsClientFactory;
        private IMessageBus _bus;
        private string _topicName;
        private string _queueName;

        protected override void Given()
        { }

        protected override IMessageBus CreateSystemUnderTest()
        {
            // Given 2 handlers
            _uniqueTopicAndQueueNames = new UniqueTopicAndQueueNames();
            _proxyAwsClientFactory = new ProxyAwsClientFactory();

            const string baseQueueName = "CustomerOrders_";
            _topicName = _uniqueTopicAndQueueNames.GetTopicName(string.Empty, typeof(Order).Name);
            _queueName = _uniqueTopicAndQueueNames.GetQueueName(new SqsReadConfiguration(SubscriptionType.ToTopic) {BaseQueueName = baseQueueName }, typeof(Order).Name);

            _bus = CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithAwsClientFactory(() => _proxyAwsClientFactory)
                .WithNamingStrategy(() => _uniqueTopicAndQueueNames)
                .WithSqsTopicSubscriber()
                .IntoQueue(baseQueueName) // generate unique queue name
                .WithMessageHandlers(new OrderHandler(), new OrderHandler())
                .Build().GetAwaiter().GetResult();

            _bus.StartListening();
            return _bus;
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
            AssertHasCounterSetToOne("CreateTopic", _topicName);
        }

        [Test]
        public void FindTopicCalledOnce()
        {
            AssertHasCounterSetToOne("FindTopic", _topicName);
        }

        [Test]
        public void ListQueuesCalledOnce()
        {
            AssertHasCounterSetToOne("ListQueues", _queueName);
        }

        [Test]
        public void CreateQueueCalledOnce()
        {
            AssertHasCounterSetToOne("CreateQueue", _queueName);
        }

        private void AssertHasCounterSetToOne(string counter, string testQueueName)
        {
            var counters = _proxyAwsClientFactory.Counters;

            Assert.That(counters.ContainsKey(counter), Is.True, "no counter: " + counter);
            Assert.That(counters[counter].ContainsKey(testQueueName), Is.True, "no queueName: " + testQueueName);
            Assert.That(counters[counter][testQueueName].Count, Is.EqualTo(1), "Wrong count");
        }
    }
}
