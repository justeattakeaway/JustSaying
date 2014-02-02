using JustEat.Simples.NotificationStack.Stack;
using NUnit.Framework;
using Tests.MessageStubs;

namespace NotificationStack.IntegrationTests.FluentNotificationStack
{
    public class WhenAMessageIsPublishedViaSnsToSqsSubscriber : GivenANotificationStack
    {
        private readonly Future<GenericMessage> _handler = new Future<GenericMessage>();

        protected override IFluentNotificationStack CreateSystemUnderTest()
        {
            base.CreateSystemUnderTest();
            ServiceBus.WithMessageHandler(_handler);
            ServiceBus.StartListening();
            return ServiceBus;
        }

        protected override void When()
        {
            ServiceBus.Publish(new GenericMessage());
        }

        [Test]
        public void ThenItGetsHandled()
        {
            _handler.WaitUntilCompletion(2.Seconds()).ShouldBeTrue();
        }

    }
}
