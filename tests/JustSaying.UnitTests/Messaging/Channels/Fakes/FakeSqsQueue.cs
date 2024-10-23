using System.Collections.Concurrent;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class FakeSqsQueue(Func<CancellationToken, Task<IEnumerable<Message>>> messageProducer, string queueName = "fake-queue-name") : ISqsQueue
{
    private readonly Func<CancellationToken, Task<IEnumerable<Message>>> _messageProducer = messageProducer;
    private readonly TaskCompletionSource _receivedAllMessages = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _messageReceived;
    private readonly ConcurrentBag<FakeDeleteMessageRequest> _deleteMessageRequests = [];
    private readonly ConcurrentBag<FakeChangeMessageVisibilityRequest> _changeMessageVisibilityRequests = [];
    private readonly ConcurrentBag<FakeTagQueueRequest> _tagQueueRequests = [];
    private readonly ConcurrentBag<FakeReceiveMessagesRequest> _receiveMessageRequests = [];

    public InterrogationResult Interrogate()
    {
        return InterrogationResult.Empty;
    }

    public string QueueName { get; } = queueName;
    public string RegionSystemName { get; } = "fake-region";
    public Uri Uri { get; set; } = new Uri("http://test.com");
    public string Arn { get; } = $"arn:aws:fake-region:123456789012:{queueName}";
    public int? MaxNumberOfMessagesToReceive { get; set; } = 100;
    public Task ReceivedAllMessages => _receivedAllMessages.Task;

    public IReadOnlyCollection<FakeDeleteMessageRequest> DeleteMessageRequests => _deleteMessageRequests;
    public IReadOnlyCollection<FakeChangeMessageVisibilityRequest> ChangeMessageVisibilityRequests => _changeMessageVisibilityRequests;
    public IReadOnlyCollection<FakeTagQueueRequest> TagQueueRequests => _tagQueueRequests;
    public IReadOnlyCollection<FakeReceiveMessagesRequest> ReceiveMessageRequests => _receiveMessageRequests;

    public Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken)
    {
        _deleteMessageRequests.Add(new FakeDeleteMessageRequest(queueUrl, receiptHandle));
        return Task.CompletedTask;
    }

    public Task TagQueueAsync(string queueUrl, Dictionary<string, string> tags, CancellationToken cancellationToken)
    {
        _tagQueueRequests.Add(new FakeTagQueueRequest(queueUrl, tags));
        return Task.CompletedTask;
    }

    public async Task<IList<Message>> ReceiveMessagesAsync(string queueUrl, int maxNumOfMessages, int secondsWaitTime, IList<string> attributesToLoad, CancellationToken cancellationToken)
    {
        await Task.Yield();
        var messages = await _messageProducer(cancellationToken);

        var countToTake = MaxNumberOfMessagesToReceive is null ? maxNumOfMessages : Math.Min(maxNumOfMessages, MaxNumberOfMessagesToReceive.Value - _messageReceived);
        var result =  messages.Take(countToTake).ToList();
        _messageReceived += result.Count;

        _receiveMessageRequests.Add(new FakeReceiveMessagesRequest(queueUrl, maxNumOfMessages, secondsWaitTime, attributesToLoad, result.Count));

        if (_messageReceived >= MaxNumberOfMessagesToReceive)
        {
            _receivedAllMessages.TrySetResult();
        }

        return result;
    }

    public Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int visibilityTimeoutInSeconds, CancellationToken cancellationToken)
    {
        _changeMessageVisibilityRequests.Add(new FakeChangeMessageVisibilityRequest(queueUrl, receiptHandle, visibilityTimeoutInSeconds));
        return Task.CompletedTask;
    }
}
