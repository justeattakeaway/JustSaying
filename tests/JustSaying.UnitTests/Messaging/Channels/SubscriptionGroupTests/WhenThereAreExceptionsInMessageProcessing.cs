using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenThereAreExceptionsInMessageProcessing(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper)
{
    private ISqsQueue _queue;
    private int _callCount;

    protected override void Given()
    {
        ConcurrencyLimit = 1;

        IEnumerable<Message> GetMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Interlocked.Increment(ref _callCount);
                yield return new TestMessage();
            }
        }

        _queue = CreateSuccessfulTestQueue("TestQueue", ct => Task.FromResult(GetMessages(ct)));

        Queues.Add(_queue);

        SerializationRegister.DefaultDeserializedMessage = () =>
            throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing");
    }

    protected override bool Until()
    {
        return _callCount > 1;
    }

    [Fact]
    public async Task TheListenerDoesNotDie()
    {
        await Patiently.AssertThatAsync(OutputHelper,
            () => _callCount.ShouldBeGreaterThan(1));
    }
}