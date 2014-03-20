using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Testing;
using NSubstitute;

namespace SimpleMessageMule.UnitTests.SimpleMessageMuleTests.AddingHandlers
{
    public class WhenAddingASubscriptionHandler : FluentMessageMuleTestBase
    {
        private readonly IHandler<Message> _handler = Substitute.For<IHandler<Message>>();
        private const string Topic = "CustomerCommunication";

        protected override void Given(){}

        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber(Topic, 60).WithMessageHandler(_handler);
        }

        [Then]
        public void HandlerIsAddedToStack()
        {
            NotificationStack.Received().AddMessageHandler(Topic, _handler);
        }
        
        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            NotificationStack.SerialisationRegister.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser<Message>>());
        }
    }
}
