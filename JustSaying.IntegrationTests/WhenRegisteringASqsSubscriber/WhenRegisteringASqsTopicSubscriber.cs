using System;
using Amazon;
using Amazon.SQS;
using JustEat.Testing;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    public class WhenRegisteringASqsTopicSubscriber : FluentNotificationStackTestBase
    {
        protected string _topicName;
        protected string _queueName;

        protected override void Given()
        {
            _topicName = "CustomerCommunication";
            _queueName = "queuename-" + DateTime.Now.Ticks;

            MockNotidicationStack();

            Configuration = new MessagingConfig
            {
                Region = DefaultRegion.SystemName
            };

            DeleteTopicIfItAlreadyExists(DefaultRegion, _topicName);
            DeleteQueueIfItAlreadyExists(DefaultRegion, _queueName);
        }

        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber(_topicName)
                .IntoQueue(_queueName)
                .ConfigureSubscriptionWith(cfg =>
            {
                cfg.MessageRetentionSeconds = 60;
            }).WithMessageHandler(Substitute.For<IHandler<Message>>());
        }

        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            NotificationStack.SerialisationRegister.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser<Message>>());
        }

        [Then, Timeout(70000)] // ToDo: Sorry about this, but SQS is a little slow to verify againse. Can be better I'm sure? ;)
        public void QueueIsCreated()
        {
            var queue = new SqsQueueByName(_queueName, new AmazonSQSClient(RegionEndpoint.EUWest1), 0);

            Patiently.AssertThat(queue.Exists, TimeSpan.FromSeconds(65));
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTopicIfItAlreadyExists(TestEndpoint, _topicName);
            DeleteQueueIfItAlreadyExists(DefaultRegion, _queueName);
        }
    }

    public class WhenRegisteringASqsTopicSubscriberUsingBasicSyntax : WhenRegisteringASqsTopicSubscriber
    {
        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber(_topicName)
                .IntoQueue(_queueName)
                .WithMessageHandler(Substitute.For<IHandler<Message>>());
        }
    }
}
