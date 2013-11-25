using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;

namespace NotificationStack.IntegrationTests.WhenConfiguringTheRegion
{
    public class WhenRegisteringAPublisher : BehaviourTest<FluentNotificationStack>
    {
        private readonly INotificationStack _stack = Substitute.For<INotificationStack>();
        private const string Topic = "CustomerCommunication";

        protected override JustEat.Simples.NotificationStack.Stack.FluentNotificationStack CreateSystemUnderTest()
        {
            return new JustEat.Simples.NotificationStack.Stack.FluentNotificationStack(_stack, null);
        }

        protected override void Given() { }

        protected override void When()
        {
            SystemUnderTest.WithSnsMessagePublisher<Message>(Topic);
        }

        [Then]
        public void APublisherIsAddedToTheStack()
        {
            _stack.Received().AddMessagePublisher<Message>(Topic, Arg.Any<IMessagePublisher>());
        }

        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            _stack.SerialisationRegister.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser<Message>>());
        }
    }
}