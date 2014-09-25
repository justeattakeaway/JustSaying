using JustSaying.TestingFramework;
using NUnit.Framework;

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
            ServiceBus.Publish(new AnotherGenericMessage {Content = "Hello"});
        }

        [Test]
        public void ThenItGetsHandled()
        {
            _handler.WaitUntilCompletion(2.Seconds()).ShouldBeTrue();
        }
    }
}