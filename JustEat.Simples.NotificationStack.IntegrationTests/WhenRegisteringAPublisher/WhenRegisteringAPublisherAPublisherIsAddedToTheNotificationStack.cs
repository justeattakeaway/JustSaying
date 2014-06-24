using JustSaying.Messaging;
using JustSaying.Stack;
using JustEat.Testing;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;

namespace NotificationStack.IntegrationTests.WhenRegisteringAPublisher
{
    public class WhenRegisteringAPublisherAPublisherIsAddedToTheNotificationStack : FluentNotificationStackTestBase
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
            SystemUnderTest.WithSnsMessagePublisher<Message>(_topicName);
        }

        [Then]
        public void APublisherIsAddedToTheStack()
        {
            NotificationStack.Received().AddMessagePublisher<Message>(_topicName, Arg.Any<IMessagePublisher>());
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTopicIfItAlreadyExists(FluentNotificationStack.DefaultEndpoint, _topicName);
        }
    }
}