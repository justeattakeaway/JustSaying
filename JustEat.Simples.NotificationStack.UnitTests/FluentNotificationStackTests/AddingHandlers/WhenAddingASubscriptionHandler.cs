using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.FluentNotificationStackTests.AddingHandlers
{
    public class WhenAddingASubscriptionHandler : BehaviourTest<FluentSubscription>
    {
        private readonly INotificationStack _stack = Substitute.For<INotificationStack>();
        private readonly IMessageSerialisationRegister _serialisationReg = Substitute.For<IMessageSerialisationRegister>();
        private readonly IHandler<Message> _handler = Substitute.For<IHandler<Message>>();
        private const string Topic = "CustomerCommunication";

        protected override FluentSubscription CreateSystemUnderTest()
        {
            return new FluentSubscription(_stack, _serialisationReg, Topic);
        }

        protected override void Given(){}

        protected override void When()
        {
            SystemUnderTest.WithMessageHandler(_handler);
        }

        [Then]
        public void HandlerIsAddedToStack()
        {
            _stack.Received().AddMessageHandler(Topic, _handler);
        }
        
        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            _serialisationReg.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser<Message>>());
        }
    }
}
