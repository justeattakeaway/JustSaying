using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public class WhenAMessageIsPublishedViaSnsToSqsSubscriber : GivenANotificationStack
    {
        private Future<GenericMessage> _handler;

        protected override void Given()
        {
            base.Given();
            _handler = new Future<GenericMessage>();
            RegisterSnsHandler(_handler);
        }

        protected override async Task When()
        {
            await ServiceBus.PublishAsync(new GenericMessage());

            await _handler.DoneSignal;
        }

        [Fact]
        public void ThenItGetsHandled()
        {
            _handler.ReceivedMessageCount.ShouldBeGreaterThan(0);
        }
    }
}
