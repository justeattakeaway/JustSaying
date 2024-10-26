using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
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

        _queue = new FakeSqsQueue(ct => Task.FromResult(GetMessages(ct)));
        var sqsSource = new SqsSource
        {
            SqsQueue = _queue,
            MessageConverter = new ReceivedMessageConverter(new ThrowingMessageBodySerializer(), new MessageCompressionRegistry(), false)
        };

        Queues.Add(sqsSource);
    }

    protected override Task<bool> UntilAsync()
    {
        return Task.FromResult(_callCount > 1);
    }

    [Fact]
    public async Task TheListenerDoesNotDie()
    {
        await Patiently.AssertThatAsync(OutputHelper,
            () => _callCount.ShouldBeGreaterThan(1));
    }

    private sealed class ThrowingMessageBodySerializer : IMessageBodySerializer
    {
        public string Serialize(Models.Message message) => throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing");
        public Models.Message Deserialize(string message) => throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing");
    }
}
