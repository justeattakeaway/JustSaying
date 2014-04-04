using JustEat.Testing;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Messages;
using JustSaying.Messaging.MessageSerialisation;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
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
                Region = DefaultRegion.SystemName
            };

            DeleteTopicIfItAlreadyExists(DefaultRegion, _topicName);
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
            DeleteTopicIfItAlreadyExists(JustSayingFluently.DefaultEndpoint, _topicName);
        }
    }
}
