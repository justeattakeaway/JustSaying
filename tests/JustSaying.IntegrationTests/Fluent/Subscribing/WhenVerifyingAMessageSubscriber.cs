using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using JustSaying.Fluent;
using JustSaying.Naming;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenVerifyingAMessageSubscriber : IntegrationTestBase
    {
        public WhenVerifyingAMessageSubscriber(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_The_Topic_Exist()
        {
            // Arrange
            var topicName = new UniqueTopicNamingConvention().TopicName<SimpleMessage>();
            await CreateRequiredInfrastructure(topicName);

            // Act - Force topic creation
            var createdOK = true;
            try
            {
                var completionSource = new TaskCompletionSource<object>();
                var handler = CreateHandler<SimpleMessage>(completionSource);
                //Create Bus that uses this topic
                var serviceProvider = GivenJustSaying()
                    .ConfigureJustSaying(
                        builder =>
                        {
                            builder.Subscriptions(options =>
                            {
                                options.ForTopic<SimpleMessage>(topicConfig =>
                                {
                                    topicConfig.WithQueue(UniqueName);
                                    topicConfig.WithTopic(topicName);
                                    topicConfig.WithInfrastructure(InfrastructureAction.ValidateExists);
                                });
                            });
                        })
                    .AddSingleton(handler)
                    .BuildServiceProvider();

                IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();
                var ctx = new CancellationTokenSource();

                await listener.StartAsync(ctx.Token);

                ctx.CancelAfter(1000);
            }
            catch (InvalidOperationException)
            {
                createdOK = false;
            }

            // Assert
            createdOK.ShouldBeTrue();

        }

        private async Task CreateRequiredInfrastructure(string topicName)
        {
            var snsClient = CreateClientFactory().GetSnsClient(Region);
            var createTopicResponse = await snsClient.CreateTopicAsync(new CreateTopicRequest()
            {
                Name = topicName,
                Tags = new List<Tag> { new Tag { Key = "Author", Value = "WhenVerifyingAMessagePublisher" } }
            });

            //sanity check
            createTopicResponse.HttpStatusCode.ShouldBe(HttpStatusCode.OK);

            //and now the required queue
            var sqsClient = CreateClientFactory().GetSqsClient(Region);
            var createQueueResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest()
            {
                QueueName = UniqueName,
                Tags = new Dictionary<string, string> { { "Author", "WhenVerifyingAMessagePublisher" } }
            });

            //sanity check
            createQueueResponse.HttpStatusCode.ShouldBe(HttpStatusCode.OK);

            //bind the queue to the topic
            var subscriptionArn = await snsClient.SubscribeQueueAsync(createTopicResponse.TopicArn, sqsClient, createQueueResponse.QueueUrl);

            //sanityCheck
            subscriptionArn.ShouldNotBeNull();
        }
    }
}
