using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Message = JustSaying.Models.Message;

namespace JustSaying.UnitTests.AwsTools.MessageHandling;

/// <summary>
/// Every message in a call is converted before anything is sent, so a message that is too large fails the
/// call without the messages ahead of it having already gone out.
/// </summary>
public class WhenABatchContainsAMessageThatIsTooLarge
{
    // Small enough that each message needs its own request, so a publisher that sent as it went would
    // have issued several requests before reaching the message that is too large.
    private const int MaximumMessageSize = 2048;

    private static List<Message> MessagesWithTheLastOneTooLarge()
    {
        var messages = new List<Message>();
        for (int i = 0; i < 4; i++)
        {
            messages.Add(new SimpleMessage { Content = new string('a', 1500) });
        }

        messages.Add(new SimpleMessage { Content = new string('a', MaximumMessageSize * 2) });
        return messages;
    }

    private static OutboundMessageConverter CreateConverter(PublishDestinationType destinationType)
        => new(
            destinationType,
            SimpleMessage.Serializer,
            new MessageCompressionRegistry(),
            new PublishCompressionOptions(),
            nameof(SimpleMessage),
            isRawMessage: true,
            MaximumMessageSize);

    [Test]
    public async Task NothingIsPublishedToTheTopic()
    {
        var sns = Substitute.For<IAmazonSimpleNotificationService>();
        var publisher = new SnsMessagePublisher("topicarn", sns, CreateConverter(PublishDestinationType.Topic), NullLoggerFactory.Instance, null, null);

        await Should.ThrowAsync<MessageTooLargeException>(
            async () => await publisher.PublishAsync(MessagesWithTheLastOneTooLarge(), null, CancellationToken.None));

        await sns.DidNotReceive().PublishBatchAsync(Arg.Any<PublishBatchRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NothingIsSentToTheQueue()
    {
        var sqs = Substitute.For<IAmazonSQS>();
        var publisher = new SqsMessagePublisher(new Uri("https://blablabla/queuename"), sqs, CreateConverter(PublishDestinationType.Queue), NullLoggerFactory.Instance);

        await Should.ThrowAsync<MessageTooLargeException>(
            async () => await publisher.PublishAsync(MessagesWithTheLastOneTooLarge(), null, CancellationToken.None));

        await sqs.DidNotReceive().SendMessageBatchAsync(Arg.Any<SendMessageBatchRequest>(), Arg.Any<CancellationToken>());
    }
}
