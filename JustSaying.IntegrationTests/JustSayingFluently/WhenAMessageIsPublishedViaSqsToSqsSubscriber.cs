using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public class WhenAMessageIsPublishedViaSqsToSqsSubscriber : GivenANotificationStack
    {
        private Future<AnotherGenericMessage> _handler;

        protected override void Given()
        {
            base.Given();
            _handler = new Future<AnotherGenericMessage>();
            RegisterSqsHandler(_handler);
        }

        protected override async Task When()
        {
            await ServiceBus.PublishAsync(new AnotherGenericMessage());
            await _handler.DoneSignal;
        }

        [Fact]
        public void ThenItGetsHandled()
        {
            _handler.ReceivedMessageCount.ShouldBeGreaterThan(0);
        }
    }
}
