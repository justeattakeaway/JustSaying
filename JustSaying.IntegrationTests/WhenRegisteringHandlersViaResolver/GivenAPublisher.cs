using System.Threading.Tasks;
using JustBehave;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public abstract class GivenAPublisher : TestingFramework.AsyncBehaviourTest<IMessagePublisher>
    {
        protected IMessagePublisher Publisher;
        protected IMessageSubscriber Subscriber;
        protected Task DoneSignal;

        protected override async Task<IMessagePublisher> CreateSystemUnderTest()
        {
            Publisher = await CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion("eu-west-1")
                .WithSnsMessagePublisher<OrderPlaced>()
                .BuildPublisherAsync();

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

        private void TearDownPubSub() => Subscriber?.StopListening();
    }
}
