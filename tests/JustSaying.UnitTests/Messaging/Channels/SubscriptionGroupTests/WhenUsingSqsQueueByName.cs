using Amazon;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;

#pragma warning disable 618

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public sealed class WhenUsingSqsQueueByName(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper), IDisposable
{
    private ISqsQueue _queue;
    private FakeAmazonSqs _client;
    readonly string MessageTypeString = nameof(SimpleMessage);
    const string MessageBody = "object";

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

        Queues.Add(_queue);
    }

    [Fact]
    public void HandlerReceivesMessage()
    {
        Handler.ReceivedMessages.Contains(SerializationRegister.DefaultDeserializedMessage())
            .ShouldBeTrue();
    }

    private static ReceiveMessageResponse GenerateResponseMessages(
        string messageType,
        Guid messageId)
    {
        return new ReceiveMessageResponse
        {
            Messages = new List<Message>
            {
                new()
                {
                    MessageId = messageId.ToString(),
                    Body = SqsMessageBody(messageType)
                },
                new()
                {
                    MessageId = messageId.ToString(),
                    Body = "{\"Subject\":\"SOME_UNKNOWN_MESSAGE\"," +
                           "\"Message\":\"SOME_RANDOM_MESSAGE\"}"
                }
            }
        };
    }

    private static string SqsMessageBody(string messageType)
    {
        return "{\"Subject\":\"" + messageType + "\"," + "\"Message\":\"" + MessageBody + "\"}";
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
