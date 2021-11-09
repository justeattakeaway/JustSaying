using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class FakeSqsQueue : ISqsQueue
{
    private readonly Func<CancellationToken, Task<IEnumerable<Message>>> _messageProducer;

    public FakeSqsQueue(Func<CancellationToken, Task<IEnumerable<Message>>> messageProducer, string queueName = "fake-queue-name")
    {
        _messageProducer = messageProducer;
        QueueName = queueName;
        RegionSystemName = "fake-region";
        Uri = new Uri("http://test.com");
        Arn = $"arn:aws:fake-region:123456789012:{queueName}";
    }

    public InterrogationResult Interrogate()
    {
        return InterrogationResult.Empty;
    }

    public string QueueName { get; }
    public string RegionSystemName { get; }
    public Uri Uri { get; set; }
    public string Arn { get; }

    public List<FakeDeleteMessageRequest> DeleteMessageRequests { get; } = new();
    public List<FakeChangeMessageVisbilityRequest> ChangeMessageVisbilityRequests { get; } = new();
    public List<FakeTagQueueRequest> TagQueueRequests { get; } = new();
    public List<FakeReceiveMessagesRequest> ReceiveMessageRequests { get; } = new();

    public Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken)
    {
        DeleteMessageRequests.Add(new FakeDeleteMessageRequest(queueUrl, receiptHandle));
        return Task.CompletedTask;
    }

    public Task TagQueueAsync(string queueUrl, Dictionary<string, string> tags, CancellationToken cancellationToken)
    {
        TagQueueRequests.Add(new FakeTagQueueRequest(queueUrl, tags));
        return Task.CompletedTask;
    }

    public async Task<IList<Message>> ReceiveMessagesAsync(string queueUrl, int maxNumOfMessages, int secondsWaitTime, IList<string> attributesToLoad, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken);
        var messages = await _messageProducer(cancellationToken);
        var result =  messages.Take(maxNumOfMessages).ToList();

        ReceiveMessageRequests.Add(new FakeReceiveMessagesRequest(queueUrl, maxNumOfMessages, secondsWaitTime, attributesToLoad, result.Count));

        return result;
    }

    public Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int visibilityTimeoutInSeconds, CancellationToken cancellationToken)
    {
        ChangeMessageVisbilityRequests.Add(new FakeChangeMessageVisbilityRequest(queueUrl, receiptHandle, visibilityTimeoutInSeconds));
        return Task.CompletedTask;
    }
}