using Amazon;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenRegisteringAPublisherWithTags : IntegrationTestBase
{
    public WhenRegisteringAPublisherWithTags(ITestOutputHelper outputHelper)
        : base(outputHelper)
    { }

    [NotSimulatorFact]
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
                    options.WithTopic<MessageForTags>((topicBuilder) =>
                    {
                        foreach ((string key, string value) in tags)
                        {
                            topicBuilder.WithTag(key, value);
                        }
                    });
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

    private static string CleanTagValue(string tagValue) => string.IsNullOrEmpty(tagValue) ? null : tagValue;

    private class MessageForTags : Message
    {
    }
}