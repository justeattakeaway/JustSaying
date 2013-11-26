using System.Threading;
using Amazon;
using Amazon.SimpleNotificationService.Model;
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

    public class WhenRegisteringASqsTopicSubscriberInANonDefaultRegion : FluentNotificationStackTestBase
    {
        private string _topicName;
        private RegionEndpoint _regionEndpoint;

        protected override void Given()
        {
            _topicName = "NonDefaultTopicSubscriptionTest";
            _regionEndpoint = RegionEndpoint.SAEast1;

            MockNotidicationStack();

            Configuration = new MessagingConfig
            {
                Component = "intergrationtestcomponent",
                Environment = "integrationtest",
                Tenant = "all",
                Region = _regionEndpoint.SystemName
            };

            DeleteQueueIfItAlreadyExists(_regionEndpoint, _topicName);
            DeleteTopicIfItAlreadyExists(_regionEndpoint, _topicName);
        }

        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber(_topicName, 60)
                           .WithMessageHandler(Substitute.For<IHandler<Message>>());
        }

        [Then]
        public void QueueIsCreatedInTheNonDefaultRegion()
        {
            Thread.Sleep(QueueCreationDelayMilliseconds);
            string temp;
            Assert.IsTrue(TryGetQueue(_regionEndpoint, _topicName, out temp));
        }

        [Then]
        public void TopicIsCreatedInTheNonDefaultRegion()
        {
            Topic temp;
            Assert.IsTrue(TryGetTopic(_regionEndpoint, _topicName, out temp));
        }

        [Then]
        public void QueueIsSubscribedToTheTopic()
        {

        }

        [Then]
        public void QueueHasPermissionsToRecieveMessagesFromTheTopic()
        {

        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            DeleteQueueIfItAlreadyExists(_regionEndpoint, _topicName);
            DeleteTopicIfItAlreadyExists(_regionEndpoint, _topicName);
        }
    }
}
