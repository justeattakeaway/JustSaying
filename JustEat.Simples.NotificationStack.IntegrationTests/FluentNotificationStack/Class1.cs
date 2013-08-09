using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;

namespace NotificationStack.IntegrationTests.FluentNotificationStack
{
    public class WhenRegisteringAPublisher : BehaviourTest<JustEat.Simples.NotificationStack.Stack.FluentNotificationStack>
    {
        private readonly INotificationStack _stack = Substitute.For<INotificationStack>();
        private readonly IMessageSerialisationRegister _serialisationReg = Substitute.For<IMessageSerialisationRegister>();
        private const NotificationTopic Topic = NotificationTopic.CustomerCommunication;

        protected override JustEat.Simples.NotificationStack.Stack.FluentNotificationStack CreateSystemUnderTest()
        {
            return new FluentSubscription(_stack, _serialisationReg, Topic);
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
            _serialisationReg.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser<Message>>());
        }
    }
}
