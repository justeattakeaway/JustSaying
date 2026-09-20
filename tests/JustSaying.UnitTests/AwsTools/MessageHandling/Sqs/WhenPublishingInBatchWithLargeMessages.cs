using Amazon.SQS.Model;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs;

/// <summary>
/// SQS validates the combined size of every entry in a batch against a fixed 1 MiB, so a batch of ten
/// messages that are individually fine can still be rejected.
/// </summary>
public class WhenPublishingInBatchWithLargeMessages : WhenPublishingTestBase
{
    private const string Url = "https://blablabla/queuename";
    private const int MessageBodySize = 150 * 1024;

    private static readonly string Message = new('a', MessageBodySize);

    private protected override Task<SqsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var messageConverter = new OutboundMessageConverter(
            PublishDestinationType.Queue,
            new FakeBodySerializer(Message),
            new MessageCompressionRegistry(),
            new PublishCompressionOptions(),
            nameof(SimpleMessage),
            isRawMessage: true,
            JustSayingConstants.DefaultSqsMaximumMessageSize);

        var queue = new SqsMessagePublisher(new Uri(Url), Sqs, messageConverter, NullLoggerFactory.Instance);
        return Task.FromResult(queue);
    }

    protected override void Given()
    {
        Sqs.GetQueueUrlAsync(Arg.Any<string>())
            .Returns(new GetQueueUrlResponse { QueueUrl = Url });
    }

    protected override async Task WhenAsync()
    {
        var messages = new List<JustSaying.Models.Message>();
        for (int i = 0; i < 10; i++)
        {
            messages.Add(new SimpleMessage { Content = $"Message {i}" });
        }

        await SystemUnderTest.PublishAsync(messages);
    }

    [Test]
    public void BatchesAreSplitBySizeNotJustCount()
    {
        // Six 150 KiB entries fit under 1 MiB, seven do not, so ten messages need two requests
        // rather than the single ten-entry request a count-only batcher would send.
        Sqs.Received(2).SendMessageBatchAsync(Arg.Any<SendMessageBatchRequest>());
    }

    [Test]
    public void NoBatchExceedsTheBatchMaximum()
    {
        Sqs.DidNotReceive().SendMessageBatchAsync(Arg.Is<SendMessageBatchRequest>(x => CombinedSize(x) > JustSayingConstants.MaximumSqsBatchPayloadSize));
    }

    [Test]
    public void EveryMessageIsStillPublished()
    {
        var published = Sqs.ReceivedCalls()
            .Where(x => x.GetMethodInfo().Name == nameof(Sqs.SendMessageBatchAsync))
            .Select(x => (SendMessageBatchRequest)x.GetArguments()[0])
            .Sum(x => x.Entries.Count);

        published.ShouldBe(10);
    }

    private static int CombinedSize(SendMessageBatchRequest request)
        => request.Entries.Sum(x => x.MessageBody.Length);
}
