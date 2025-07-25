using System.Text.Json;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs;

public class WhenPublishingInBatch : WhenPublishingTestBase
{
    private readonly OutboundMessageConverter _outboundMessageConverter = CreateConverter();
    private const string Url = "https://blablabla/" + QueueName;
    private readonly List<SimpleMessage> _messages = new();
    private const string QueueName = "queuename";

    private protected override Task<SqsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var sqs = new SqsMessagePublisher(new Uri(Url), Sqs, _outboundMessageConverter, Substitute.For<ILoggerFactory>());
        return Task.FromResult(sqs);
    }

    protected override void Given()
    {
        for (var i = 0; i < 1_000; i++)
        {
            _messages.Add(new SimpleMessage{ Content = $"Message_{i}" });
        }

        Sqs.GetQueueUrlAsync(Arg.Any<string>())
            .Returns(new GetQueueUrlResponse { QueueUrl = Url });

        Sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
            .Returns(new GetQueueAttributesResponse());
    }

    protected override async Task WhenAsync()
    {
        await SystemUnderTest.PublishAsync(_messages);
    }

    [Fact]
    public void MultipleMessagesIsPublishedToQueue()
    {
        Sqs.Received(100).SendMessageBatchAsync(Arg.Any<SendMessageBatchRequest>());
    }

    [Fact]
    public void MessageIsPublishedToQueue()
    {
        Sqs.Received().SendMessageBatchAsync(Arg.Is<SendMessageBatchRequest>(x => AssertMessageIsPublishedToQueue(x)));
    }

    private static bool AssertMessageIsPublishedToQueue(SendMessageBatchRequest request)
    {
        if (!request.Entries.Count.Equals(10))
        {
            return false;
        }

        for (var i = 0; i < 10; i++)
        {
            var entry = request.Entries[i];
            if (!entry.MessageBody.Contains(JsonEncodedText.Encode(
                    $"""
                    "Content":"Message_{i}
                    """).Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    [Fact]
    public void MessageIsPublishedToCorrectLocation()
    {
        Sqs.Received().SendMessageBatchAsync(Arg.Is<SendMessageBatchRequest>(x => x.QueueUrl == Url));
    }
}
