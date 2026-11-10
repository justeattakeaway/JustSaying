using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.TopicByName;

public class WhenPublishingInBatchWithFailedEntries : WhenPublishingTestBase
{
    private const string TopicArn = "topicarn";

    private readonly SimpleMessage _first = new();
    private readonly SimpleMessage _second = new();
    private readonly MyMessage _poco = new();
    private readonly MyMessage _secondPoco = new();
    private readonly FakeLogCollector _logs = new();

    private PublishBatchRequest _request;
    private MessageBatchResponse _response;

    private protected override Task<SnsMessagePublisher> CreateSystemUnderTestAsync()
    {
        var loggerFactory = LoggerFactory.Create(lf => lf.AddProvider(new FakeLoggerProvider(_logs)));
        var messageConverter = CreateConverter(new FakeBodySerializer("the_message_in_json"));
        var topic = new SnsMessagePublisher(TopicArn, Sns, messageConverter, loggerFactory, null, null)
        {
            MessageBatchResponseLogger = (r, _) => _response = r
        };

        return Task.FromResult(topic);
    }

    protected override void Given()
    {
        Sns.PublishBatchAsync(Arg.Any<PublishBatchRequest>())
            .Returns(call =>
            {
                _request = call.Arg<PublishBatchRequest>();
                return new PublishBatchResponse
                {
                    Successful =
                    [
                        new PublishBatchResultEntry { Id = "0", MessageId = "sns-0" },
                        new PublishBatchResultEntry { Id = "3", MessageId = "sns-3" },
                    ],
                    Failed =
                    [
                        new BatchResultErrorEntry { Id = "1", Code = "InternalError" },
                        new BatchResultErrorEntry { Id = "2", Code = "InternalError" },
                    ],
                };
            });
    }

    protected override Task WhenAsync()
        => SystemUnderTest.PublishBatchAsync<object>([_first, _second, _poco, _secondPoco], null, CancellationToken.None);

    [Test]
    public void EntryIdsArePositionalRatherThanDerivedFromTheMessage()
    {
        _request.PublishBatchRequestEntries.Select(e => e.Id).ShouldBe(["0", "1", "2", "3"]);
    }

    [Test]
    public void FailedEntriesAreMappedBackToTheirMessages()
    {
        _response.SuccessfulMessageIds.ShouldBe(["sns-0", "sns-3"]);
        _response.FailedMessageIds.ShouldBe([_second.Id.ToString(), "2"]);
    }

    [Test]
    public void LogsNameTheMessagesTheyReferTo()
    {
        var messages = _logs.GetSnapshot().Select(r => r.Message).ToList();

        messages.ShouldContain($"Published message {_first.Id} of type {typeof(SimpleMessage).FullName} to Topic '{TopicArn}'.");
        messages.ShouldContain(m => m.StartsWith($"Failed to publish message {_second.Id} (batch entry 1) to Topic", StringComparison.Ordinal));
    }

    [Test]
    public void LogsDoNotReportTheEntryIdAsTheIdOfAMessageWithoutOne()
    {
        var messages = _logs.GetSnapshot().Select(r => r.Message).ToList();

        messages.ShouldContain($"Published message (no id) of type {typeof(MyMessage).FullName} to Topic '{TopicArn}'.");
        messages.ShouldContain(m => m.StartsWith("Failed to publish message (no id) (batch entry 2) to Topic", StringComparison.Ordinal));
    }
}
