using System;
using System.Threading.Tasks;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenRegisteringASqsTopicSubscriber : FluentNotificationStackTestBase
    {
        protected string TopicName { get; set; }

        protected string QueueName { get; set; }

        protected IAmazonSQS Client { get; set; }

        protected override void Given()
        {
            base.Given();

            TopicName = "CustomerCommunication";
            QueueName = "queuename-" + DateTime.Now.Ticks;

            EnableMockedBus();

            Configuration = new MessagingConfig();

            DeleteTopicIfItAlreadyExists(TestEndpoint, TopicName).Wait();
            DeleteQueueIfItAlreadyExists(TestEndpoint, QueueName).Wait();

            Client = CreateMeABus.DefaultClientFactory().GetSqsClient(TestEnvironment.Region);
        }

        protected override Task When()
        {
            SystemUnderTest.WithSqsTopicSubscriber()
                .IntoQueue(QueueName)
                .ConfigureSubscriptionWith(cfg => cfg.MessageRetentionSeconds = 60)
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
                var queue = new SqsQueueByName(
                    TestEnvironment.Region,
                    QueueName,
                    Client,
                    0,
                    NullLoggerFactory.Instance);

                await Patiently.AssertThatAsync(
                    () => queue.ExistsAsync(), TimeSpan.FromSeconds(65));
            }

            var task = QueueIsCreatedInner();

            if (task == await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(70))))
            {
                await task;
            }
            else
            {
                throw new TimeoutException();
            }
        }

        protected override void PostAssertTeardown()
        {
            DeleteTopicIfItAlreadyExists(TestEndpoint, TopicName).Wait();
            DeleteQueueIfItAlreadyExists(TestEndpoint, QueueName).Wait();
        }
    }
}
