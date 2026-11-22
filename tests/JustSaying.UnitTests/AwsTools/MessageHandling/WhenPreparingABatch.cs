using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Message = JustSaying.Models.Message;
using MessageAttributeValue = JustSaying.Messaging.MessageAttributeValue;

namespace JustSaying.UnitTests.AwsTools.MessageHandling;

public class WhenPreparingABatch
{
    private static SnsMessagePublisher CreatePublisher(string topicArn, IAmazonSimpleNotificationService sns)
        => new(
            topicArn,
            sns,
            new OutboundMessageConverter(
                PublishDestinationType.Topic,
                SimpleMessage.Serializer,
                new MessageCompressionRegistry(),
                new PublishCompressionOptions(),
                nameof(SimpleMessage),
                isRawMessage: false,
                JustSayingConstants.DefaultSnsMaximumMessageSize),
            NullLoggerFactory.Instance,
            null,
            null);

    [Test]
    public async Task NothingIsSentUntilABatchIsSent()
    {
        var sns = Substitute.For<IAmazonSimpleNotificationService>();
        var publisher = CreatePublisher("topicarn", sns);

        var batches = await publisher.PrepareAsync([new SimpleMessage(), new SimpleMessage()], null, CancellationToken.None);

        batches.Count.ShouldBe(1);
        batches[0].Messages.Count.ShouldBe(2);
        await sns.DidNotReceive().PublishBatchAsync(Arg.Any<PublishBatchRequest>(), Arg.Any<CancellationToken>());

        await batches[0].SendAsync(CancellationToken.None);

        await sns.Received(1).PublishBatchAsync(Arg.Is<PublishBatchRequest>(x => x.PublishBatchRequestEntries.Count == 2), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A binary attribute is handed to the AWS SDK as a stream. A batch that is sent again after a failure
    /// has to give it one that has not already been read.
    /// </summary>
    [Test]
    public async Task ABatchCanBeSentAgain()
    {
        var sns = Substitute.For<IAmazonSimpleNotificationService>();
        var binaryValuesSeen = new List<byte[]>();

        sns.PublishBatchAsync(Arg.Any<PublishBatchRequest>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                // Read the stream to the end, as the SDK is free to do when it marshals the request.
                using var buffer = new MemoryStream();
                x.Arg<PublishBatchRequest>().PublishBatchRequestEntries[0].MessageAttributes["Binary"].BinaryValue.CopyTo(buffer);
                binaryValuesSeen.Add(buffer.ToArray());
                return new PublishBatchResponse();
            });

        var metadata = new PublishBatchMetadata();
        metadata.AddMessageAttribute("Binary", new MessageAttributeValue { DataType = "Binary", BinaryValue = [1, 2, 3] });

        var batches = await CreatePublisher("topicarn", sns).PrepareAsync([new SimpleMessage()], metadata, CancellationToken.None);

        await batches[0].SendAsync(CancellationToken.None);
        await batches[0].SendAsync(CancellationToken.None);

        binaryValuesSeen.Count.ShouldBe(2);
        binaryValuesSeen[0].ShouldBe([1, 2, 3]);
        binaryValuesSeen[1].ShouldBe([1, 2, 3]);
    }

    [Test]
    public async Task ADynamicTopicAddressPreparesTheBatchesOfEachTopic()
    {
        var sns = Substitute.For<IAmazonSimpleNotificationService>();

        var publisher = new DynamicAddressMessagePublisher(
            "arn:aws:sns:eu-west-1:123456789012:{tenant}",
            (template, message) => template.Replace("{tenant}", ((SimpleMessage)message).Content),
            topicArn =>
            {
                var topicPublisher = CreatePublisher(topicArn, sns);
                return new StaticAddressPublicationConfiguration(topicPublisher, topicPublisher);
            },
            NullLoggerFactory.Instance);

        List<Message> messages =
        [
            new SimpleMessage { Content = "one" },
            new SimpleMessage { Content = "two" },
            new SimpleMessage { Content = "one" },
        ];

        var batches = await publisher.PrepareAsync(messages, null, CancellationToken.None);

        batches.Select(x => x.Messages.Count).ShouldBe([2, 1]);
        await sns.DidNotReceive().PublishBatchAsync(Arg.Any<PublishBatchRequest>(), Arg.Any<CancellationToken>());

        foreach (var batch in batches)
        {
            await batch.SendAsync(CancellationToken.None);
        }

        await sns.Received(1).PublishBatchAsync(Arg.Is<PublishBatchRequest>(x => x.TopicArn.EndsWith(":one") && x.PublishBatchRequestEntries.Count == 2), Arg.Any<CancellationToken>());
        await sns.Received(1).PublishBatchAsync(Arg.Is<PublishBatchRequest>(x => x.TopicArn.EndsWith(":two") && x.PublishBatchRequestEntries.Count == 1), Arg.Any<CancellationToken>());
    }
}
