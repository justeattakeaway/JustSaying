using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;
using SimpleMessageMule;

namespace NotificationStack.IntegrationTests.WhenRegisteringAPublisher
{
    public class WhenRegisteringAPublisherASerialiserIsAddedForTheMessageType : FluentNotificationStackTestBase
    {
        private string _topicName;

        protected override void Given()
        {
            _topicName = "CustomerCommunication";

            MockNotidicationStack();

            Configuration = new MessagingConfig
            {
                Region = DefaultRegion.SystemName
            };

            DeleteTopicIfItAlreadyExists(FluentMessagingMule.DefaultEndpoint, _topicName);
        }

        protected override void When()
        {
            SystemUnderTest.WithSnsMessagePublisher<Message>(_topicName);
        }

        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            NotificationStack.SerialisationRegister.Received()
                .AddSerialiser<Message>(Arg.Any<IMessageSerialiser<Message>>());
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTopicIfItAlreadyExists(FluentMessagingMule.DefaultEndpoint, _topicName);
        }
    }
}