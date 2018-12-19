using System;

namespace JustSaying.Messaging.Monitoring
{
    public class NullOpMessageMonitor : IMessageMonitor
    {
        public void HandleException(Type messageType) { }

        public void HandleTime(TimeSpan duration) { }

        public void IssuePublishingMessage() { }

        public void IncrementThrottlingStatistic() { }

        public void HandleThrottlingTime(TimeSpan duration) { }

        public void PublishMessageTime(TimeSpan duration) { }

        public void ReceiveMessageTime(TimeSpan duration, string queueName, string region) { }
    }
}
