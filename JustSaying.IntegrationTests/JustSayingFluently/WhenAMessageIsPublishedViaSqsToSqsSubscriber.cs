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

        protected override void When()
        {
            ServiceBus.Publish(new AnotherGenericMessage());
        }

        [Test]
        public void ThenItGetsHandled()
        {
            _handler.WaitUntilCompletion(15.Seconds()).ShouldBe(true);
        }
    }
}