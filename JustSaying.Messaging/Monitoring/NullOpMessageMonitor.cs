namespace JustSaying.Messaging.Monitoring
{
    public class NullOpMessageMonitor : IMessageMonitor
    {
        public void HandleException(string messageType) { }

        public void HandleTime(long handleTimeMs) { }

        public void IssuePublishingMessage() { }

        public void IncrementThrottlingStatistic() { }

        public void HandleThrottlingTime(long handleTimeMs) { }

        public void PublishMessageTime(long handleTimeMs) { }

        public void ReceiveMessageTime(long handleTimeMs, string name, string queueName) { }
    }
}