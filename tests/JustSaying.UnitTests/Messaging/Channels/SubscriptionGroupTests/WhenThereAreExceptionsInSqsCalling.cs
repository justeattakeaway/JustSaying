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
        _queue = CreateSuccessfulTestQueue("TestQueue", ExceptionOnFirstCall());
        Queues.Add(_queue);

        SerializationRegister.DefaultDeserializedMessage =
            () => throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing");
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

    protected override bool Until()
    {
        return _callCount > 1;
    }

    [Fact]
    public void QueueIsPolledMoreThanOnce()
    {
        _callCount.ShouldBeGreaterThan(1);
    }
}