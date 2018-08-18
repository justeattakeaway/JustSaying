using System;
using JustBehave;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenSettingUpMultipleHandlersFails : XBehaviourTest<IHaveFulfilledSubscriptionRequirements>
    {
        protected string QueueUniqueKey;
        private ProxyAwsClientFactory proxyAwsClientFactory;
        private int handlersAttached = 0;
        private NotSupportedException _capturedException;

        protected override void Given()
        {
        }

        protected override void When()
        {
        }

        protected override IHaveFulfilledSubscriptionRequirements CreateSystemUnderTest()
        {
            proxyAwsClientFactory = new ProxyAwsClientFactory();

            var busConfig = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(TestEnvironment.Region.SystemName)
                .WithAwsClientFactory(() => proxyAwsClientFactory);

            try
            {
                // Second distinct handler declaration for the same queue should fail
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
            _capturedException.Message.ShouldStartWith("The handler for 'JustSaying.TestingFramework.Order' messages on this queue has already been registered.");
        }
    }
}
