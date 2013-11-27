using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace NotificationStack.IntegrationTests.WhenRegisteringASqsSubscriber
{
    public class WhenRegisteringASqsTopicSubscriber : FluentNotificationStackTestBase
    {
        private string _topicName;

        protected override void Given()
        {
            _topicName = "CustomerCommunication";

            MockNotidicationStack();

            Configuration = new MessagingConfig
            {
                Component = "intergrationtestcomponent",
                Environment = "integrationtest",
                Tenant = "all"
            };

            DeleteTopicIfItAlreadyExists(FluentNotificationStack.DefaultEndpoint, _topicName);
        }

        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber(_topicName, 60).WithMessageHandler(Substitute.For<IHandler<Message>>());
        }

        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            NotificationStack.SerialisationRegister.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser<Message>>());
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTopicIfItAlreadyExists(FluentNotificationStack.DefaultEndpoint, _topicName);
        }
    }
}
