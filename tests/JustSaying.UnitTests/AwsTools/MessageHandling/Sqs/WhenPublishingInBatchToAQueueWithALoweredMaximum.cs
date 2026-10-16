using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs;

/// <summary>
/// A queue's MaximumMessageSize limits each message, but unlike an SNS topic it does not lower what a
/// batch may add up to, which stays at 1 MiB. Checked against SQS: ten 100 KiB messages are accepted
/// in one request by a queue with a MaximumMessageSize of 256 KiB.
/// </summary>
public class WhenPublishingInBatchToAQueueWithALoweredMaximum : WhenPublishingTestBase
{
    private const string Url = "https://blablabla/queuename";

    private static readonly string Message = new('a', 100 * 1024);

    private protected override Task<SqsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var messageConverter = new OutboundMessageConverter(
            PublishDestinationType.Queue,
            new FakeBodySerializer(Message),
            new MessageCompressionRegistry(),
            new PublishCompressionOptions(),
            nameof(SimpleMessage),
            isRawMessage: true,
            maximumMessageSize: 256 * 1024);

        var queue = new SqsMessagePublisher(new Uri(Url), Sqs, messageConverter, NullLoggerFactory.Instance);
        return Task.FromResult(queue);
    }

    protected override void Given()
    {
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
    public void TheBatchIsNotSplit()
    {
        Sqs.Received(1).SendMessageBatchAsync(Arg.Is<SendMessageBatchRequest>(x => x.Entries.Count == 10));
    }
}
