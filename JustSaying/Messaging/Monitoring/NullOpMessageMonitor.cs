using System;

namespace JustSaying.Messaging.Monitoring
{
    public class NullOpMessageMonitor : IMessageMonitor
    {
        public void HandleException(Type messageType) { }

        public void HandleTime(TimeSpan handleTime) { }

        public void IssuePublishingMessage() { }

        public void IncrementThrottlingStatistic() { }

        public void HandleThrottlingTime(TimeSpan handleTime) { }

        public void PublishMessageTime(TimeSpan handleTime) { }

        public void ReceiveMessageTime(TimeSpan handleTime, string queueName, string region) { }
    }
}
