using System.Diagnostics.CodeAnalysis;
using Amazon.SQS.Model;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenMessageHandlingSucceeds(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper)
{
    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string MessageBody = """
                                       {
                                         "Subject": "TestMessage",
                                         "Message": "Expected Message Body"
                                       }
                                       """;
    private FakeSqsQueue _queue;

    protected override void Given()
    {
        var sqsSource = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(), new TestMessage { Body = MessageBody });
        _queue = sqsSource.SqsQueue as FakeSqsQueue;

        Queues.Add(sqsSource);
    }

    protected override bool Until()
    {
        _queue.ReceivedAllMessages.Wait();
         CompletionMiddleware.Complete?.Wait();
        return base.Until();
    }

    [Fact]
    public void ProcessingIsPassedToTheHandlerForCorrectMessage()
    {
        Handler.ReceivedMessages.ShouldContain(SetupMessage);
    }

    [Fact]
    public async Task AllMessagesAreClearedFromQueue()
    {
        await Patiently.AssertThatAsync(() => _queue.DeleteMessageRequests.Count.ShouldBe(Handler.ReceivedMessages.Count));
    }

    [Fact]
    public void ReceiveMessageTimeStatsSent()
    {
        var numberOfMessagesHandled = Handler.ReceivedMessages.Count;

        // The receive buffer might receive messages that aren't handled before shutdown
        Monitor.ReceiveMessageTimes.Count.ShouldBeGreaterThanOrEqualTo(numberOfMessagesHandled);
    }

    [Fact]
    public void ExceptionIsNotLoggedToMonitor()
    {
        Monitor.HandledExceptions.ShouldBeEmpty();
    }
}
