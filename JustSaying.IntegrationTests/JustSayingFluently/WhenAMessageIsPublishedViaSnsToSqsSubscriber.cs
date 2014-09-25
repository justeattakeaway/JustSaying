using JustSaying.TestingFramework;
using NUnit.Framework;
using Shouldly;

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

        protected override void When()
        {
            ServiceBus.Publish(new GenericMessage());
        }

        [Test]
        public void ThenItGetsHandled()
        {
            _handler.WaitUntilCompletion(10.Seconds()).ShouldBe(true);
        }
    }
}
