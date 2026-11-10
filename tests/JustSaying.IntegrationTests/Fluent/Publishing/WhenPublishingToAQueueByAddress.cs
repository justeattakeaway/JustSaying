using JustSaying.AwsTools.MessageHandling;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenPublishingToAQueueByAddress : IntegrationTestBase
{
    [Test]
    public async Task Then_The_Bus_Metadata_Provider_And_Batch_Response_Logger_Are_Used()
    {
        // Arrange
        var queueUrl = (await CreateClientFactory().GetSqsClient(Region).CreateQueueAsync(UniqueName)).QueueUrl;

        var metadataProvider = new RecordingMetadataProvider();
        var batchResponses = new List<MessageBatchResponse>();

        var services = Given((builder, serviceProvider) =>
        {
            var config = serviceProvider.GetRequiredService<MessagingConfig>();
            config.MessageMetadataProvider = metadataProvider;
            config.MessageBatchResponseLogger = (response, _) => batchResponses.Add(response);

            builder.Publications((options) => options.WithQueue<SimpleMessage>(QueueDestination.FromUrl(queueUrl)));
        });

        await WhenBatchAsync(
            services,
            async (publisher, _, cancellationToken) =>
            {
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishBatchAsync([new SimpleMessage(), new SimpleMessage()], cancellationToken);
            });

        // Assert
        metadataProvider.Calls.ShouldBeGreaterThan(0);
        batchResponses.Count.ShouldBe(1);
    }

    private sealed class RecordingMetadataProvider : IMessageMetadataProvider
    {
        private int _calls;

        public int Calls => _calls;

        public string GetId(object message)
        {
            Interlocked.Increment(ref _calls);
            return DefaultMessageMetadataProvider.Instance.GetId(message);
        }

        public DateTimeOffset? GetTimestamp(object message)
        {
            Interlocked.Increment(ref _calls);
            return DefaultMessageMetadataProvider.Instance.GetTimestamp(message);
        }

        public bool TryGetDeduplicationKey(object message, out string deduplicationKey)
        {
            Interlocked.Increment(ref _calls);
            return DefaultMessageMetadataProvider.Instance.TryGetDeduplicationKey(message, out deduplicationKey);
        }
    }
}
