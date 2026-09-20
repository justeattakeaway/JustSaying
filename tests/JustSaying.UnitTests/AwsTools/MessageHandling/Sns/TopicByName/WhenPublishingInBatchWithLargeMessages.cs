using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Models;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

/// <summary>
/// SNS validates the combined size of every entry in a batch against the topic's MaximumMessageSize,
/// so a batch of ten messages that are individually fine can still be rejected.
/// </summary>
public class WhenPublishingInBatchWithLargeMessages : WhenPublishingTestBase
{
    private const string TopicArn = "topicarn";
    private const int MessageBodySize = 100 * 1024;

    private static readonly string Message = new('a', MessageBodySize);

    private protected override Task<SnsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var messageConverter = new OutboundMessageConverter(
            PublishDestinationType.Topic,
            new FakeBodySerializer(Message),
            new MessageCompressionRegistry(),
            new PublishCompressionOptions(),
            nameof(SimpleMessage),
            false,
            JustSayingConstants.DefaultSnsMaximumMessageSize);

        var topic = new SnsMessagePublisher(TopicArn, Sns, messageConverter, NullLoggerFactory.Instance, null, null);
        return Task.FromResult(topic);
    }

    protected override void Given()
    {
        Sns.FindTopicAsync("TopicName")
            .Returns(new Topic { TopicArn = TopicArn });
    }

    protected override async Task WhenAsync()
    {
        var messages = new List<Message>();
        for (int i = 0; i < 10; i++)
        {
            messages.Add(new SimpleMessage { Content = $"Message {i}" });
        }

        await SystemUnderTest.PublishAsync(messages);
    }

    [Test]
    public void BatchesAreSplitBySizeNotJustCount()
    {
        // Two 100 KiB entries fit under 256 KiB, three do not, so ten messages need five requests
        // rather than the single ten-entry request a count-only batcher would send.
        Sns.Received(5).PublishBatchAsync(Arg.Any<PublishBatchRequest>());
    }

    [Test]
    public void NoBatchExceedsTheTopicMaximum()
    {
        Sns.DidNotReceive().PublishBatchAsync(Arg.Is<PublishBatchRequest>(x => CombinedSize(x) > JustSayingConstants.DefaultSnsMaximumMessageSize));
    }

    [Test]
    public void EveryMessageIsStillPublished()
    {
        var published = Sns.ReceivedCalls()
            .Where(x => x.GetMethodInfo().Name == nameof(Sns.PublishBatchAsync))
            .Select(x => (PublishBatchRequest)x.GetArguments()[0])
            .Sum(x => x.PublishBatchRequestEntries.Count);

        published.ShouldBe(10);
    }

    private static int CombinedSize(PublishBatchRequest request)
        => request.PublishBatchRequestEntries.Sum(x => x.Message.Length + (x.Subject?.Length ?? 0));
}
