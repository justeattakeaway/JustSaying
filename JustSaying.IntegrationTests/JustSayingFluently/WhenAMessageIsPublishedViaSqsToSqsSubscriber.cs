using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.TestingFramework;
using NUnit.Framework;
using Shouldly;

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
            ServiceBus.Publish(new AnotherGenericMessage());
            await _handler.DoneSignal;
        }

        [Test]
        public void ThenItGetsHandled()
        {
            _handler.ReceivedMessageCount.ShouldBeGreaterThan(0);
        }
    }
}