using System;

namespace JustSaying.Messaging.Monitoring
{
    public interface IMessageMonitor
    {
        void HandleException(Type messageType);
        void HandleTime(TimeSpan duration);
        void IssuePublishingMessage();
        void IncrementThrottlingStatistic();
        void HandleThrottlingTime(TimeSpan duration);
        void PublishMessageTime(TimeSpan duration);
        void ReceiveMessageTime(TimeSpan duration, string queueName, string region);
    }
}
