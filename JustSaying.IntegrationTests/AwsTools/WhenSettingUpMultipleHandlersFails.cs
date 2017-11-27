using System;
using System.Threading.Tasks;
using Amazon;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    public class WhenSettingUpMultipleHandlersFails : XBehaviourTest<IHaveFulfilledSubscriptionRequirements>
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
        private ProxyAwsClientFactory proxyAwsClientFactory;
        private int handlersAttached = 0;
        private NotSupportedException _capturedException;

        protected override void Given()
        { }

        protected override void When()
        {
        }

        protected override IHaveFulfilledSubscriptionRequirements CreateSystemUnderTest()
        {
            proxyAwsClientFactory = new ProxyAwsClientFactory();

            var busConfig = CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithAwsClientFactory(() => proxyAwsClientFactory);
            try
            {
                // 2nd distinct handler declaration for the same queue should fail
                AttachAHandler(busConfig);
                AttachAHandler(busConfig);
            }
            catch (NotSupportedException ex)
            {
                _capturedException = ex;
            }
            return null;
        }

        private void AttachAHandler(IMayWantOptionalSettings busConfig)
        {
            busConfig
                .WithSqsTopicSubscriber()
                .IntoDefaultQueue()
                .WithMessageHandler(new OrderHandler());
            handlersAttached++;
        }

        [Fact]
        public void ThenOnlyOneHandlerIsAttached()
        {
            handlersAttached.ShouldBe(1);
        }

        [Fact]
        public void ThenAnExceptionIsThrown()
        {
            _capturedException.ShouldNotBeNull();
            _capturedException.Message.ShouldStartWith("The handler for 'Order' messages on this queue has already been registered.");
        }
    }
}
