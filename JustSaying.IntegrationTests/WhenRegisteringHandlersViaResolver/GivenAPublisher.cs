using System.Threading;
using System.Threading.Tasks;
using JustBehave;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Shouldly;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public abstract class GivenAPublisher : XAsyncBehaviourTest<IMessagePublisher>
    {
        protected IHaveFulfilledPublishRequirements Publisher { get; set; }

        protected IHaveFulfilledSubscriptionRequirements Subscriber { get; set; }

        protected CancellationTokenSource PublisherCts { get; set; }
        protected CancellationTokenSource SubscriberCts { get; set; }

        protected Task DoneSignal { get; set; }

        protected override IMessagePublisher CreateSystemUnderTest()
        {
            var fixture = new JustSayingFixture();

            Publisher = fixture.Builder()
                .WithSnsMessagePublisher<OrderPlaced>();

            PublisherCts = new CancellationTokenSource();
            Publisher.StartListening(PublisherCts.Token);

            return Publisher;
        }

        protected override async Task When()
        {
            await Publisher.PublishAsync(new OrderPlaced("1234"));

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
            done.ShouldBe(true, "Done task timed out");
        }

        private void TearDownPubSub()
        {
            PublisherCts?.Cancel();
            SubscriberCts?.Cancel();
        }
    }
}
