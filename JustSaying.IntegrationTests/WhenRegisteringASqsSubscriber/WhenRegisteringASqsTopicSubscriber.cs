using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    public class WhenRegisteringASqsTopicSubscriber : FluentNotificationStackTestBase
    {
        protected string TopicName;
        protected string QueueName;
        protected IAmazonSQS Client;

        protected override void Given()
        {
            base.Given();

            TopicName = "CustomerCommunication";
            QueueName = "queuename-" + DateTime.Now.Ticks;

            EnableMockedBus();

            Configuration = new MessagingConfig();

            DeleteTopicIfItAlreadyExists(TestEndpoint, TopicName);
            DeleteQueueIfItAlreadyExists(TestEndpoint, QueueName);
            Client = CreateMeABus.DefaultClientFactory().GetSqsClient(RegionEndpoint.EUWest1);
        }

        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber()
                .IntoQueue(QueueName)
                .ConfigureSubscriptionWith(cfg =>
                    {
                        cfg.MessageRetentionSeconds = 60;
                    })
                .WithMessageHandler(Substitute.For<IHandlerAsync<Message>>());
        }

        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            NotificationStack.SerialisationRegister.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser>());
        }

        [Then, Timeout(70000)] // ToDo: Sorry about this, but SQS is a little slow to verify against. Can be better I'm sure? ;)
        public async Task QueueIsCreated()
        {
            var queue = new SqsQueueByName(RegionEndpoint.EUWest1, 
                QueueName, Client, 0, Substitute.For<ILoggerFactory>());

            await Patiently.AssertThatAsync(
                queue.Exists, TimeSpan.FromSeconds(65));
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTopicIfItAlreadyExists(TestEndpoint, TopicName);
            DeleteQueueIfItAlreadyExists(TestEndpoint, QueueName);
        }
    }

    public class WhenRegisteringASqsTopicSubscriberUsingBasicSyntax : WhenRegisteringASqsTopicSubscriber
    {
        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber()
                .IntoQueue(QueueName)
                .WithMessageHandler(Substitute.For<IHandlerAsync<Message>>());
        }
    }
}
