using System.Collections.Generic;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class ReceiveMessagesRequest
    {
        public ReceiveMessagesRequest(string queueUrl, int maxNumOfMessages, int secondsWaitTime, IList<string> attributesToLoad)
        {
            QueueUrl = queueUrl;
            MaxNumOfMessages = maxNumOfMessages;
            SecondsWaitTime = secondsWaitTime;
            AttributesToLoad = attributesToLoad;
        }

        public string QueueUrl { get; }
        public int MaxNumOfMessages { get; }
        public int SecondsWaitTime { get; }
        public IList<string> AttributesToLoad { get; }
    }
}
