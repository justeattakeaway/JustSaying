using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Util;
using JustSaying.AwsTools;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class AddressPubSub : IntegrationTestBase
    {
        public AddressPubSub(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task SimplePubSubWorks()
        {
            IAwsClientFactory clientFactory = CreateClientFactory();
            var sqsClient = clientFactory.GetSqsClient(Region);
            var snsClient = clientFactory.GetSnsClient(Region);
            var queueResponse = await sqsClient.CreateQueueAsync(UniqueName);
            var anotherUniqueName = $"{Guid.NewGuid():N}-integration-tests";
            var topicResponse = await snsClient.CreateTopicAsync(anotherUniqueName);
            var subscriptionArn = await snsClient.SubscribeQueueAsync(topicResponse.TopicArn, sqsClient, queueResponse.QueueUrl);

            var handler = new InspectableHandler<SimpleMessage>();

            var services = GivenJustSaying()
                .ConfigureJustSaying(builder =>
                    builder
                        .Subscriptions(c =>
                            c.ForQueueUrl<SimpleMessage>(queueResponse.QueueUrl))
                        .Publications(c =>
                            c.WithTopicArn<SimpleMessage>(topicResponse.TopicArn)
                        )
                )
                .AddJustSayingHandlers(new[] { handler });

            string content = Guid.NewGuid().ToString();

            var message = new SimpleMessage
            {
                Content = content
            };

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);
                    await publisher.StartAsync(cancellationToken);

                    await publisher.PublishAsync(message, cancellationToken);

                    // Assert
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                    handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(content);
                });
        }

        [AwsFact]
        public async Task CanSubscribeUsingQueueArn()
        {
            IAwsClientFactory clientFactory = CreateClientFactory();
            var sqsClient = clientFactory.GetSqsClient(Region);
            var snsClient = clientFactory.GetSnsClient(Region);
            var queueResponse = await sqsClient.CreateQueueAsync(UniqueName);
            var anotherUniqueName = $"{Guid.NewGuid():N}-integration-tests";
            var topicResponse = await snsClient.CreateTopicAsync(anotherUniqueName);
            var subscriptionArn = await snsClient.SubscribeQueueAsync(topicResponse.TopicArn, sqsClient, queueResponse.QueueUrl);
            var queueArn = (await sqsClient.GetQueueAttributesAsync(queueResponse.QueueUrl, new List<string> { SQSConstants.ATTRIBUTE_QUEUE_ARN })).Attributes[SQSConstants.ATTRIBUTE_QUEUE_ARN];

            var handler = new InspectableHandler<SimpleMessage>();

            var services = GivenJustSaying()
                .ConfigureJustSaying(builder =>
                    builder
                        .Subscriptions(c =>
                            c.ForQueueArn<SimpleMessage>(queueArn))
                        .Publications(c =>
                            c.WithTopicArn<SimpleMessage>(topicResponse.TopicArn)
                        )
                )
                .AddJustSayingHandlers(new[] { handler });

            string content = Guid.NewGuid().ToString();

            var message = new SimpleMessage
            {
                Content = content
            };

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);
                    await publisher.StartAsync(cancellationToken);

                    await publisher.PublishAsync(message, cancellationToken);

                    // Assert
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                    handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(content);
                });
        }

        [AwsFact]
        public async Task CanPublishUsingQueueUrl()
        {
            IAwsClientFactory clientFactory = CreateClientFactory();
            var sqsClient = clientFactory.GetSqsClient(Region);
            var queueResponse = await sqsClient.CreateQueueAsync(UniqueName);

            var handler = new InspectableHandler<SimpleMessage>();

            var services = GivenJustSaying()
                .ConfigureJustSaying(builder =>
                    builder
                        .Subscriptions(c =>
                            c.ForQueueUrl<SimpleMessage>(queueResponse.QueueUrl))
                        .Publications(c =>
                            c.WithQueueUrl<SimpleMessage>(queueResponse.QueueUrl)
                        )
                )
                .AddJustSayingHandlers(new[] { handler });

            string content = Guid.NewGuid().ToString();

            var message = new SimpleMessage
            {
                Content = content
            };

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);
                    await publisher.StartAsync(cancellationToken);

                    await publisher.PublishAsync(message, cancellationToken);

                    // Assert
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                    handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(content);
                });
        }
    }
}
