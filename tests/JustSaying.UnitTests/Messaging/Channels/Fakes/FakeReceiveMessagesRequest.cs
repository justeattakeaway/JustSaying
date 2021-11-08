namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class FakeReceiveMessagesRequest
{
    public FakeReceiveMessagesRequest(string queueUrl, int maxNumOfMessages, int secondsWaitTime, IList<string> attributesToLoad, int numMessagesReceived)
    {
        QueueUrl = queueUrl;
        MaxNumOfMessages = maxNumOfMessages;
        SecondsWaitTime = secondsWaitTime;
        AttributesToLoad = attributesToLoad;
        NumMessagesReceived = numMessagesReceived;
    }

    public string QueueUrl { get; }
    public int MaxNumOfMessages { get; }
    public int NumMessagesReceived { get; }
    public int SecondsWaitTime { get; }
    public IList<string> AttributesToLoad { get; }
}