using System.Threading.Tasks;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public abstract class GivenAPublisher : AsyncBehaviourTest<IMessagePublisher>
    {
        protected IHaveFulfilledPublishRequirements Publisher;
        protected IHaveFulfilledSubscriptionRequirements Subscriber;
        protected Task DoneSignal;

        protected override IMessagePublisher CreateSystemUnderTest()
        {
            Publisher = JustSaying.CreateMeABus.InRegion("eu-west-1")
                .WithSnsMessagePublisher<OrderPlaced>();
            Publisher.StartListening();
            return Publisher;
        }

        protected override async Task When()
        {
            Publisher.Publish(new OrderPlaced("1234"));

            await WaitForDone();

            TearDownPubSub();
        }

        private async Task WaitForDone()
        {
            if (DoneSignal == null)
            {
                return;
            }

            var done = await Tasks.WaitWithTimeoutAsync(DoneSignal);
            if (!done)
            {
                Assert.Fail("Done task timed out");
            }
        }

        private void TearDownPubSub()
        {
            if (Publisher != null)
            {
                Publisher.StopListening();
            }
            if (Subscriber != null)
            {
                Subscriber.StopListening();
            }
        }

    }
}