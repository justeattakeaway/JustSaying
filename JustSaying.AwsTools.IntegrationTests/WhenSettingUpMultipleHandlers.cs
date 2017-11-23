using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageHandling;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    [TestFixture]
    public class WhenSettingUpMultipleHandlers : BehaviourTest<IHaveFulfilledSubscriptionRequirements>
    {
        public class Order : JustSaying.Models.Message
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
            queueName = uniqueTopicAndQueueNames.GetQueueName(new SqsReadConfiguration(SubscriptionType.ToTopic) { BaseQueueName = baseQueueName }, typeof(Order).Name);

            bus = CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithAwsClientFactory(() => proxyAwsClientFactory)
                .WithNamingStrategy(() => uniqueTopicAndQueueNames)
                .WithSqsTopicSubscriber()
                .IntoQueue(baseQueueName) // generate unique queue name
                .WithMessageHandler<Order>(new OrderHandler())
                .WithMessageHandler<Order>(new OrderHandler());

            bus
                .StartListening();
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
        public void CreateTopicCalled()
        {
            Assert.That(proxyAwsClientFactory.Counters["CreateTopic"][topicName].Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void GetQueueAttributesCalledOnce()
        {
            Assert.That(proxyAwsClientFactory.Counters["GetQueueAttributes"].First(x => x.Key.EndsWith(queueName)).Value.Count, Is.EqualTo(1));
        }

        [Test]
        public void CreateQueueCalledOnce()
        {
            Assert.That(proxyAwsClientFactory.Counters["CreateQueue"][queueName].Count, Is.EqualTo(1));
        }

        [Test]
        public void SetQueueAttributesCalledOnceWithBasicAttributes()
        {
            var allSetQueueAttributesCalls = proxyAwsClientFactory.Counters["SetQueueAttributes"]
                .Where(x => x.Key.EndsWith(queueName))
                .SelectMany(x => x.Value)
                .Select(x => (SetQueueAttributesRequest) ((object[]) x).First())
                .ToArray();
           
            Assert.That(allSetQueueAttributesCalls
                .Count(x => x.Attributes.ContainsKey(SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT)), Is.EqualTo(1));
        }
    }
}
