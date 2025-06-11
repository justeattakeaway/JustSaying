using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenThereAreExceptionsInSqsCalling(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper)
{
    private ISqsQueue _queue;
    private int _callCount;

    protected override void Given()
    {
        var sqsSource = CreateSuccessfulTestQueue("TestQueue", ExceptionOnFirstCall());
        _queue = sqsSource.SqsQueue as FakeSqsQueue;
        Queues.Add(sqsSource);

        // setup deserializer failure
    }

    private IEnumerable<Message> ExceptionOnFirstCall()
    {
        _callCount++;
        if (_callCount == 1)
        {
            throw new TestException("testing the failure on first call");
        }

        yield break;
    }

    protected override Task<bool> UntilAsync()
    {
        return Task.FromResult(_callCount > 1);
    }

    [Fact]
    public void QueueIsPolledMoreThanOnce()
    {
        _callCount.ShouldBeGreaterThan(1);
    }
}
