using System.Text.Json;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenListeningStartsAndStops(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper)
{
    private int _expectedMaxMessageCount;
    private bool _running;
    private FakeSqsQueue _queue;

    protected override void Given()
    {
        // we expect to get max 10 messages per batch
        _expectedMaxMessageCount = MessageDefaults.MaxAmazonMessageCap;

        Logger.LogInformation("Expected max message count is {MaxMessageCount}", _expectedMaxMessageCount);

        var response1 = new Message { Body = $$"""{ "Subject": "SimpleMessage", "Message": "{{JsonEncodedText.Encode(JsonSerializer.Serialize(new SimpleMessage { Content = "Message Contents Running" }))}}" }""" };
        var response2 = new Message { Body = $$"""{ "Subject": "SimpleMessage", "Message": "{{JsonEncodedText.Encode(JsonSerializer.Serialize(new SimpleMessage { Content = "Message Contents After Stop" }))}}" }""" };
        IEnumerable<Message> GetMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_running) yield return response1;
                else yield return response2;
            }
        }

        var sqsQueue = new FakeSqsQueue(ct => Task.FromResult(GetMessages(ct)), Guid.NewGuid().ToString());

        var sqsSource = new SqsSource
        {
            SqsQueue = sqsQueue,
            MessageConverter = new ReceivedMessageConverter(new SystemTextJsonMessageBodySerializer<SimpleMessage>(), new MessageCompressionRegistry(), false)
        };

        _queue = sqsSource.SqsQueue as FakeSqsQueue;

        Queues.Add(sqsSource);
    }

    protected override async Task WhenAsync()
    {
        _running = true;

        await base.WhenAsync();

        _running = false;
    }

    [Fact]
    public void MessagesAreReceived()
    {
        _queue.ReceiveMessageRequests.ShouldNotBeEmpty();
    }

    [Fact]
    public void TheMaxMessageAllowanceIsGrabbed()
    {
        _queue.ReceiveMessageRequests.ShouldAllBe(req => req.MaxNumOfMessages == _expectedMaxMessageCount);
    }

    [Fact]
    public void MessageIsProcessed()
    {
        Handler.ReceivedMessages.ShouldContain(m => m.Content.Equals("Message Contents Running"));
        Handler.ReceivedMessages.ShouldNotContain(m => m.Content.Equals("Message Contents After Stop"));
    }
}
