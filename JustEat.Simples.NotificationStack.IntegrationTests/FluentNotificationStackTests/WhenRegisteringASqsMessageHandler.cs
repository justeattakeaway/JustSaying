using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Simples.NotificationStack.Stack.Amazon;
using JustEat.Testing;
using NSubstitute;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    public class WhenRegisteringASqsMessageHandler : BehaviourTest<FluentNotificationStack>
    {
        private readonly INotificationStack _stack = Substitute.For<INotificationStack>();
        private const string Topic = "CustomerCommunication";

        protected override FluentNotificationStack CreateSystemUnderTest()
        {
            return new FluentNotificationStack(_stack, Substitute.For<IVerifyAmazonQueues>());
        }

        protected override void Given() { }

        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber(Topic, 60).WithMessageHandler<Message>(Substitute.For<IHandler<Message>>());
        }

        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            _stack.SerialisationRegister.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser<Message>>());
        }
    }
}
