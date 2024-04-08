using Amazon.SQS.Util;
using JustSaying.AwsTools;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class AddressPubSub(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
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
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(content);
                    });
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
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(content);
                    });
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
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(content);
                    });
            });
    }

    [AwsFact]
    public async Task CanPublishUsingTopicArnWithoutStartingBusAndWithNoRegion()
    {
        IAwsClientFactory clientFactory = CreateClientFactory();
        var snsClient = clientFactory.GetSnsClient(Region);
        var topicResponse = await snsClient.CreateTopicAsync(UniqueName);

        var services = new ServiceCollection()
            .AddLogging((p) => p.AddXUnit(OutputHelper, o => o.IncludeScopes = true).SetMinimumLevel(LogLevel.Debug))
            .AddJustSaying(
                (builder, serviceProvider) =>
                {
                    builder.Client((options) =>
                    {
                        options.WithSessionCredentials(AccessKeyId, SecretAccessKey, SessionToken)
                            .WithServiceUri(ServiceUri);
                    });
                })
            .ConfigureJustSaying(builder =>
                builder
                    .Publications(c =>
                        c.WithTopicArn<SimpleMessage>(topicResponse.TopicArn)
                    )
            );

        string content = Guid.NewGuid().ToString();

        var message = new SimpleMessage
        {
            Content = content
        };

        await WhenAsync(
            services,
            async (publisher, listener, serviceProvider, cancellationToken) =>
            {
                // Assert does not throw
                await publisher.PublishAsync(message, cancellationToken);
            });

        await WhenBatchAsync(
            services,
            async (publisher, listener, serviceProvider, cancellationToken) =>
            {
                // Assert does not throw
                await publisher.PublishAsync([message], cancellationToken);
            });
    }

    [AwsFact]
    public async Task CanPublishUsingTopicArnWithoutStartingBusAndWithNoRegionWithPublisherWrapper()
    {
        // Arrange
        IAwsClientFactory clientFactory = CreateClientFactory();
        var snsClient = clientFactory.GetSnsClient(Region);
        var topicResponse = await snsClient.CreateTopicAsync(UniqueName);

        var services = new ServiceCollection()
            .AddLogging((p) => p.AddXUnit(OutputHelper, o => o.IncludeScopes = true).SetMinimumLevel(LogLevel.Debug))
            .AddTransient((_) => Substitute.For<IMessagePublisher>())
            .AddJustSaying(
                (builder, serviceProvider) =>
                {
                    builder.Client((options) =>
                    {
                        options.WithSessionCredentials(AccessKeyId, SecretAccessKey, SessionToken)
                               .WithServiceUri(ServiceUri);
                    });
                })
            .ConfigureJustSaying(builder =>
                builder
                    .Publications(c =>
                        c.WithTopicArn<SimpleMessage>(topicResponse.TopicArn)
                    )
            );

        using var provider = services.BuildServiceProvider();

        // Act
        var publisher = provider.GetRequiredService<IMessageBatchPublisher>();

        // Assert
        publisher.ShouldNotBeNull();
    }
}
