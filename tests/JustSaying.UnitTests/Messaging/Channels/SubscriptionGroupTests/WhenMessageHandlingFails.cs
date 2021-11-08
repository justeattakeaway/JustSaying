using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenMessageHandlingFails : BaseSubscriptionGroupTests
{
    private FakeSqsQueue _queue;

    public WhenMessageHandlingFails(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    protected override void Given()
    {
        _queue = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(), new TestMessage());

        Queues.Add(_queue);
        Handler.ShouldSucceed = false;
    }

    [Fact]
    public void MessageHandlerWasCalled()
    {
        Handler.ReceivedMessages.ShouldNotBeEmpty();
    }

    [Fact]
    public void FailedMessageIsNotRemovedFromQueue()
    {
        _queue.DeleteMessageRequests.ShouldBeEmpty();
    }

    [Fact]
    public void ExceptionIsNotLoggedToMonitor()
    {
        Monitor.HandledExceptions.ShouldBeEmpty();
    }
}