using System;
using System.Threading.Tasks;
using JustBehave;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenSettingUpMultipleHandlersFails : XAsyncBehaviourTest<IHaveFulfilledSubscriptionRequirements>
    {
        private int _handlersAttached = 0;
        private NotSupportedException _capturedException;

        protected override Task Given() => Task.CompletedTask;

        protected override Task When() => Task.CompletedTask;

        protected override Task<IHaveFulfilledSubscriptionRequirements> CreateSystemUnderTestAsync()
        {
            var awsClientFactory = new ProxyAwsClientFactory();
            var fixture = new JustSayingFixture();

            var busConfig = fixture.Builder()
                .WithAwsClientFactory(() => awsClientFactory);

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

            return Task.FromResult<IHaveFulfilledSubscriptionRequirements>(null);
        }

        private void AttachAHandler(IMayWantOptionalSettings busConfig)
        {
            busConfig
                .WithSqsTopicSubscriber()
                .IntoDefaultQueue()
                .WithMessageHandler(new OrderHandler());

            _handlersAttached++;
        }

        [AwsFact]
        public void ThenOnlyOneHandlerIsAttached()
        {
            _handlersAttached.ShouldBe(1);
        }

        [AwsFact]
        public void ThenAnExceptionIsThrown()
        {
            _capturedException.ShouldNotBeNull();
            _capturedException.Message.ShouldStartWith("The handler for 'JustSaying.TestingFramework.Order' messages on this queue has already been registered.");
        }
    }
}
