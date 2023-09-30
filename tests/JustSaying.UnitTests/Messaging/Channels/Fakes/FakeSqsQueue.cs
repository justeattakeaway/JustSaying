using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class FakeSqsQueue(Func<CancellationToken, Task<IEnumerable<Message>>> messageProducer, string queueName = "fake-queue-name") : ISqsQueue
{
    private readonly Func<CancellationToken, Task<IEnumerable<Message>>> _messageProducer = messageProducer;

    public InterrogationResult Interrogate()
    {
        return InterrogationResult.Empty;
    }

    public string QueueName { get; } = queueName;
    public string RegionSystemName { get; } = "fake-region";
    public Uri Uri { get; set; } = new Uri("http://test.com");
    public string Arn { get; } = $"arn:aws:fake-region:123456789012:{queueName}";

    public List<FakeDeleteMessageRequest> DeleteMessageRequests { get; } = new();
    public List<FakeChangeMessageVisibilityRequest> ChangeMessageVisbilityRequests { get; } = new();
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
        ChangeMessageVisbilityRequests.Add(new FakeChangeMessageVisibilityRequest(queueUrl, receiptHandle, visibilityTimeoutInSeconds));
        return Task.CompletedTask;
    }
}