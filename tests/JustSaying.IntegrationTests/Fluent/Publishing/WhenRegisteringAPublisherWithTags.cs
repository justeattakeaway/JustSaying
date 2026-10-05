using Amazon;
using Amazon.SimpleNotificationService.Model;
using JustSaying.IntegrationTests;
using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using JustSaying.Fluent;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenRegisteringAPublisherWithTags : IntegrationTestBase
{
    private const string QueueName = "message-for-tags-publisher-queue";

    [NotSimulatorSkip]
    [Test]
    public async Task Then_A_Topic_Is_Created_With_The_Correct_Tags()
    {
        // Arrange
        var tags = new Dictionary<string, string>
        {
            [Guid.NewGuid().ToString()] = null,
            [Guid.NewGuid().ToString()] = "Value"
        };

        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
                builder.Publications((options) =>
                {
                    options.WithTopic<MessageForTags>(TopicDestination.ByConvention((topic) =>
                    {
                        foreach ((string key, string value) in tags)
                        {
                            topic.WithTag(key, value);
                        }
                    }));
                }))
            .BuildServiceProvider();

        // Act
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        await publisher.StartAsync(CancellationToken.None);

        // Assert
        var busBuilder = serviceProvider.GetRequiredService<MessagingBusBuilder>();
        var clientFactory = busBuilder.BuildClientFactory();

        var client = clientFactory.GetSnsClient(RegionEndpoint.EUWest1);

        var topicArn = (await client.GetAllTopics())
            .Select((p) => p.TopicArn)
            .SingleOrDefault((p) => p.EndsWith($":{nameof(MessageForTags)}", StringComparison.OrdinalIgnoreCase));

        var topicTags = await client.ListTagsForResourceAsync(new ListTagsForResourceRequest { ResourceArn = topicArn });

        foreach (var tag in tags)
        {
            topicTags.Tags.ShouldContain((t) => t.Key == tag.Key && t.Value == CleanTagValue(tag.Value));
        }
    }

    [NotSimulatorSkip]
    [Test]
    public async Task Then_A_Queue_Is_Created_With_The_Correct_Tags()
    {
        // Arrange
        var tags = new Dictionary<string, string>
        {
            [Guid.NewGuid().ToString()] = null,
            [Guid.NewGuid().ToString()] = "Value"
        };

        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
                builder.Publications((options) =>
                {
                    options.WithQueue<MessageForTags>(QueueDestination.Named(QueueName, (queue) =>
                    {
                        foreach ((string key, string value) in tags)
                        {
                            queue.WithTag(key, value);
                        }
                    }));
                }))
            .BuildServiceProvider();

        // Act
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        await publisher.StartAsync(CancellationToken.None);

        // Assert
        var busBuilder = serviceProvider.GetRequiredService<MessagingBusBuilder>();
        var clientFactory = busBuilder.BuildClientFactory();

        var client = clientFactory.GetSqsClient(RegionEndpoint.EUWest1);

        var queueUrl = (await client.GetQueueUrlAsync(QueueName)).QueueUrl;
        var queueTags = (await client.ListQueueTagsAsync(new Amazon.SQS.Model.ListQueueTagsRequest { QueueUrl = queueUrl })).Tags;

        foreach (var tag in tags)
        {
            queueTags.ShouldContain((t) => t.Key == tag.Key && t.Value == CleanTagValue(tag.Value));
        }
    }

    private static string CleanTagValue(string tagValue) => string.IsNullOrEmpty(tagValue) ? null : tagValue;

    private class MessageForTags : Message
    {
    }
}