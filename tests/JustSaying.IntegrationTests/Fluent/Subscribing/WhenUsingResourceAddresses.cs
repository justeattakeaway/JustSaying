using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Util;
using JustSaying.AwsTools;
using JustSaying.Fluent;
using JustSaying.Naming;
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
            var topicResponse = await snsClient.CreateTopicAsync(UniqueName);
            var subscriptionArn = await snsClient.SubscribeQueueAsync(topicResponse.TopicArn, sqsClient, queueResponse.QueueUrl);

            var handler = new InspectableHandler<SimpleMessage>();

            var services = GivenJustSaying()
                .ConfigureJustSaying(builder =>
                    builder
                        .Subscriptions(c =>
                            c.ForQueue<SimpleMessage>(QueueAddress.FromUrl(queueResponse.QueueUrl, RegionName)))
                        .Publications(c =>
                            c.WithTopic<SimpleMessage>(TopicAddress.FromArn(topicResponse.TopicArn))
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
            var topicResponse = await snsClient.CreateTopicAsync(UniqueName);
            var subscriptionArn = await snsClient.SubscribeQueueAsync(topicResponse.TopicArn, sqsClient, queueResponse.QueueUrl);
            var queueArn = (await sqsClient.GetQueueAttributesAsync(queueResponse.QueueUrl, new List<string> { SQSConstants.ATTRIBUTE_QUEUE_ARN })).Attributes[SQSConstants.ATTRIBUTE_QUEUE_ARN];

            var handler = new InspectableHandler<SimpleMessage>();

            var services = GivenJustSaying()
                .ConfigureJustSaying(builder =>
                    builder
                        .Subscriptions(c =>
                            c.ForQueue<SimpleMessage>(QueueAddress.FromArn(queueArn)))
                        .Publications(c =>
                            c.WithTopic<SimpleMessage>(TopicAddress.FromArn(topicResponse.TopicArn))
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
        public async Task UsingAddressNoneWillUseClassicSubscriptionBuilder()
        {
            var queueName = UniqueName;
            var accountId = "000000000000";
            var names = new ManualNamingConvention(queueName, null);

            IAwsClientFactory clientFactory = CreateClientFactory();
            var sqsClient = clientFactory.GetSqsClient(Region);

            var handler = new InspectableHandler<SimpleMessage>();

            var services = GivenJustSaying()
                .ConfigureJustSaying(builder =>
                    builder
                        .Messaging(c =>
                            c.WithQueueNamingConvention(names)
                                .WithTopicNamingConvention(names)
                                .WithRegion(Region))
                        .Subscriptions(c =>
                            c.ForQueue<SimpleMessage>(QueueAddress.None))
                )
                .AddJustSayingHandlers(new[] { handler });

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);

                    // Assert
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                    var assumedQueueUrl = $"{ServiceUri.AbsoluteUri}/{accountId}/{queueName}";
                    var queueArn = (await sqsClient.GetQueueAttributesAsync(assumedQueueUrl, new List<string> { SQSConstants.ATTRIBUTE_QUEUE_ARN }, cancellationToken)).Attributes[SQSConstants.ATTRIBUTE_QUEUE_ARN];
                    queueArn.ShouldNotBeNullOrEmpty();
                });
        }

        private class ManualNamingConvention : IQueueNamingConvention, ITopicNamingConvention
        {
            private readonly string _queueName;
            private readonly string _topicName;

            public ManualNamingConvention(string queueName, string topicName)
            {
                _queueName = queueName;
                _topicName = topicName;
            }

            public string QueueName<T>() => _queueName;
            public string TopicName<T>() => _topicName;
        }
    }
}