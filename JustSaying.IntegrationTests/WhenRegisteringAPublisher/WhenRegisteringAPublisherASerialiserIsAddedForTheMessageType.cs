using JustEat.Testing;
using JustSaying.Messaging.Messages;
using JustSaying.Messaging.MessageSerialisation;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringAPublisher
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

            DeleteTopicIfItAlreadyExists(JustSayingFluently.DefaultEndpoint, _topicName);
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
            DeleteTopicIfItAlreadyExists(JustSayingFluently.DefaultEndpoint, _topicName);
        }
    }
}