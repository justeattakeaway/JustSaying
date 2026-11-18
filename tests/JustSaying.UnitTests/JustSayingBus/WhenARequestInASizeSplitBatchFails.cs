using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

/// <summary>
/// Ten 100 KiB messages to a 256 KiB topic need five requests of two. When one of them fails it should be
/// retried on its own, as retrying the ten messages would publish again the ones SNS had already accepted.
/// </summary>
public class WhenARequestInASizeSplitBatchFails : GivenAServiceBus
{
    private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();
    private readonly List<List<string>> _requests = [];
    private readonly List<SimpleMessage> _messages = [];

    protected override void Given()
    {
        base.Given();

        Config = Substitute.For<IMessagingConfig, IPublishBatchConfiguration>();
        Config.PublishFailureBackoff.Returns(TimeSpan.Zero);
        ((IPublishBatchConfiguration)Config).PublishFailureReAttempts.Returns(3);

        int calls = 0;
        _sns.PublishBatchAsync(Arg.Any<PublishBatchRequest>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                _requests.Add([.. x.Arg<PublishBatchRequest>().PublishBatchRequestEntries.Select(e => e.Id)]);

                // Fail the third request, the first time it is made.
                return ++calls == 3
                    ? Task.FromException<PublishBatchResponse>(new AmazonServiceException("Thrown by test"))
                    : Task.FromResult(new PublishBatchResponse());
            });

        for (int i = 0; i < 10; i++)
        {
            _messages.Add(new SimpleMessage { Content = $"Message {i}" });
        }
    }

    protected override async Task WhenAsync()
    {
        var converter = new OutboundMessageConverter(
            PublishDestinationType.Topic,
            new FakeBodySerializer(new string('a', 100 * 1024)),
            new MessageCompressionRegistry(),
            new PublishCompressionOptions(),
            nameof(SimpleMessage),
            isRawMessage: false,
            JustSayingConstants.DefaultSnsMaximumMessageSize);

        var publisher = new SnsMessagePublisher("topicarn", _sns, converter, NullLoggerFactory.Instance, null, null);
        SystemUnderTest.AddMessageBatchPublisher<SimpleMessage>(publisher);

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        await SystemUnderTest.PublishAsync(_messages, new PublishBatchMetadata(), cts.Token);
    }

    [Test]
    public void OnlyTheFailedRequestIsMadeAgain()
    {
        // Five requests, plus one retry of the third.
        _requests.Count.ShouldBe(6);
        _requests[3].ShouldBe(_requests[2]);
    }

    [Test]
    public void NoMessageIsPublishedTwice()
    {
        var accepted = _requests.Where((_, index) => index != 2).SelectMany(x => x).ToList();

        accepted.ShouldBeUnique();
        accepted.Count.ShouldBe(10);
    }
}
