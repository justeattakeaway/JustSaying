using Amazon;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;

#pragma warning disable 618

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public sealed class WhenUsingSqsQueueByName(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper), IDisposable
{
    private ISqsQueue _queue;
    private FakeAmazonSqs _client;
    readonly string MessageTypeString = nameof(SimpleMessage);
    const string MessageBody = "object";

    private readonly SimpleMessage _message = new()
    {
        RaisingComponent = "Component",
        Id = Guid.NewGuid()
    };

    protected override void Given()
    {
        int retryCount = 1;

        _client = new FakeAmazonSqs(() =>
        {
            return new[] { GenerateResponseMessages(MessageTypeString, Guid.NewGuid()) }
                .Concat(new ReceiveMessageResponse().Infinite());
        });

        var queue = new SqsQueueByName(RegionEndpoint.EUWest1,
            "some-queue-name",
            _client,
            retryCount,
            LoggerFactory);
        queue.ExistsAsync(CancellationToken.None).Wait();

        _queue = queue;

        Queues.Add(new SqsSource
        {
            SqsQueue = queue,
            MessageConverter = new InboundMessageConverter(new FakeBodyDeserializer(_message), new MessageCompressionRegistry(), false)
        });
    }

    [Fact]
    public void HandlerReceivesMessage()
    {
        Handler.ReceivedMessages.Contains(_message)
            .ShouldBeTrue();
    }

    private static ReceiveMessageResponse GenerateResponseMessages(
        string messageType,
        Guid messageId)
    {
        return new ReceiveMessageResponse
        {
            Messages =
            [
                new Message
                {
                    MessageId = messageId.ToString(),
                    Body = SqsMessageBody(messageType)
                },
                new Message
                {
                    MessageId = messageId.ToString(),
                    Body = """{"Subject":"SOME_UNKNOWN_MESSAGE","Message":"SOME_RANDOM_MESSAGE"}"""
                }
            ]
        };
    }

    private static string SqsMessageBody(string messageType)
    {
        return $$"""{"Subject":"{{messageType}}","Message":"{{MessageBody}}"}""";
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
