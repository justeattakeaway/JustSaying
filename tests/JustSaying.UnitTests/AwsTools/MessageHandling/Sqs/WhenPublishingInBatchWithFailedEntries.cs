using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs;

public class WhenPublishingInBatchWithFailedEntries : WhenPublishingTestBase
{
    private const string Url = "https://blablabla/queuename";

    private readonly SimpleMessage _first = new();
    private readonly SimpleMessage _second = new();
    private readonly MyMessage _poco = new();
    private readonly FakeLogCollector _logs = new();

    private SendMessageBatchRequest _request;
    private MessageBatchResponse _response;

    private protected override Task<SqsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var loggerFactory = LoggerFactory.Create(lf => lf.AddProvider(new FakeLoggerProvider(_logs)));
        var messageConverter = new OutboundMessageConverter(
            PublishDestinationType.Queue,
            new FakeBodySerializer("the_message_in_json"),
            new MessageCompressionRegistry(),
            new PublishCompressionOptions(),
            nameof(SimpleMessage),
            false);

        var sqs = new SqsMessagePublisher(new Uri(Url), Sqs, messageConverter, loggerFactory)
        {
            MessageBatchResponseLogger = (r, _) => _response = r
        };

        return Task.FromResult(sqs);
    }

    protected override void Given()
    {
        Sqs.SendMessageBatchAsync(Arg.Any<SendMessageBatchRequest>())
            .Returns(call =>
            {
                _request = call.Arg<SendMessageBatchRequest>();
                return new SendMessageBatchResponse
                {
                    Successful = [new SendMessageBatchResultEntry { Id = "0", MessageId = "sqs-0" }],
                    Failed =
                    [
                        new BatchResultErrorEntry { Id = "1", Code = "InternalError" },
                        new BatchResultErrorEntry { Id = "2", Code = "InternalError" },
                    ],
                };
            });
    }

    protected override Task WhenAsync()
        => SystemUnderTest.PublishBatchAsync<object>([_first, _second, _poco], null, CancellationToken.None);

    [Test]
    public void EntryIdsArePositionalRatherThanDerivedFromTheMessage()
    {
        _request.Entries.Select(e => e.Id).ShouldBe(["0", "1", "2"]);
    }

    [Test]
    public void FailedEntriesAreMappedBackToTheirMessages()
    {
        _response.SuccessfulMessageIds.ShouldBe(["sqs-0"]);
        _response.FailedMessageIds.ShouldBe([_second.Id.ToString(), "2"]);
    }

    [Test]
    public void LogsNameTheMessagesTheyReferTo()
    {
        var messages = _logs.GetSnapshot().Select(r => r.Message).ToList();

        messages.ShouldContain($"Published message {_first.Id} of type {typeof(SimpleMessage).FullName} to Queue '{Url}'.");
        messages.ShouldContain(m => m.StartsWith($"Failed to publish message {_second.Id} to Queue", StringComparison.Ordinal));
    }
}
