using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    [Collection(GlobalSetup.CollectionName)]
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

            DeleteTopicIfItAlreadyExists(TestEndpoint, TopicName).Wait();
            DeleteQueueIfItAlreadyExists(TestEndpoint, QueueName).Wait();
            Client = CreateMeABus.DefaultClientFactory().GetSqsClient(RegionEndpoint.EUWest1);
        }

        protected override Task When()
        {
            SystemUnderTest.WithSqsTopicSubscriber()
                .IntoQueue(QueueName)
                .ConfigureSubscriptionWith(cfg =>
                    {
                        cfg.MessageRetentionSeconds = 60;
                    })
                .WithMessageHandler(Substitute.For<IHandlerAsync<Message>>());

            return Task.CompletedTask;
        }

        [Fact]
        public void SerialisationIsRegisteredForMessage()
        {
            NotificationStack.SerialisationRegister.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser>());
        }

        [Fact]
        public async Task QueueIsCreated()
        {
            async Task QueueIsCreatedInner()
            {
                var queue = new SqsQueueByName(RegionEndpoint.EUWest1,
                    QueueName, Client, 0, Substitute.For<ILoggerFactory>());

                await Patiently.AssertThatAsync(
                    () => queue.ExistsAsync(), TimeSpan.FromSeconds(65));
            }

            var task = QueueIsCreatedInner();

            if (task == await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(70)))) // ToDo: Sorry about this, but SQS is a little slow to verify against. Can be better I'm sure? ;)
                await task;
            else
                throw new TimeoutException();
        }

        protected override void PostAssertTeardown()
        {
            DeleteTopicIfItAlreadyExists(TestEndpoint, TopicName).Wait();
            DeleteQueueIfItAlreadyExists(TestEndpoint, QueueName).Wait();
        }
    }

    public class WhenRegisteringASqsTopicSubscriberUsingBasicSyntax : WhenRegisteringASqsTopicSubscriber
    {
        protected override Task When()
        {
            SystemUnderTest.WithSqsTopicSubscriber()
                .IntoQueue(QueueName)
                .WithMessageHandler(Substitute.For<IHandlerAsync<Message>>());

            return Task.CompletedTask;
        }
    }
}
