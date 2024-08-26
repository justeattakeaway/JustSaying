using System.Diagnostics.CodeAnalysis;
using Amazon.SQS.Model;
using JustSaying.Messaging.Channels.SubscriptionGroups;

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

    [Fact]
    public void MessagesGetDeserializedByCorrectHandler()
    {
        // SerializationRegister.ReceivedDeserializationRequests.ShouldAllBe(
        //     msg => msg == MessageBody);
        // TODO
        throw new NotImplementedException();
    }

    [Fact]
    public void ProcessingIsPassedToTheHandlerForCorrectMessage()
    {
        //Handler.ReceivedMessages.ShouldContain(SerializationRegister.DefaultDeserializedMessage());
        // TODO
        throw new NotImplementedException();
    }

    [Fact]
    public void AllMessagesAreClearedFromQueue()
    {
        _queue.DeleteMessageRequests.Count.ShouldBe(Handler.ReceivedMessages.Count);
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
