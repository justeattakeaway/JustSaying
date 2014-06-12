using JustEat.Testing;
using JustSaying.Messaging.Messages;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Stack;
using NSubstitute;
using NUnit.Framework;
using JustSaying.Messaging.MessageHandling;

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
            SystemUnderTest.WithSqsTopicSubscriber(_topicName).IntoQueue(_topicName).WithMessageHandler(Substitute.For<IHandler<Message>>());
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
