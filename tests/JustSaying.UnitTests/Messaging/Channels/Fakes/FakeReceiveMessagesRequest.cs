namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class FakeReceiveMessagesRequest(string queueUrl, int maxNumOfMessages, int secondsWaitTime, IList<string> attributesToLoad, int numMessagesReceived)
{
    public string QueueUrl { get; } = queueUrl;
    public int MaxNumOfMessages { get; } = maxNumOfMessages;
    public int NumMessagesReceived { get; } = numMessagesReceived;
    public int SecondsWaitTime { get; } = secondsWaitTime;
    public IList<string> AttributesToLoad { get; } = attributesToLoad;
}