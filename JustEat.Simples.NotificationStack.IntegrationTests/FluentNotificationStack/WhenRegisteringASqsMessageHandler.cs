using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;

namespace NotificationStack.IntegrationTests.FluentNotificationStack
{
    public class WhenRegisteringASqsMessageHandler : BehaviourTest<JustEat.Simples.NotificationStack.Stack.FluentNotificationStack>
    {
        private readonly INotificationStack _stack = Substitute.For<INotificationStack>();
        private readonly IMessageSerialisationRegister _serialisationReg = Substitute.For<IMessageSerialisationRegister>();
        private const string Topic = "CustomerCommunication";

        protected override JustEat.Simples.NotificationStack.Stack.FluentNotificationStack CreateSystemUnderTest()
        {
            return new FluentSubscription(_stack, _serialisationReg, Topic);
        }

        protected override void Given() { }

        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber(Topic, 60).WithMessageHandler<Message>(Substitute.For<IHandler<Message>>());
        }

        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            _serialisationReg.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser<Message>>());
        }
    }
}
