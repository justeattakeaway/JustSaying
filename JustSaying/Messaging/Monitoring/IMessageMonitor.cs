using System;

namespace JustSaying.Messaging.Monitoring
{
    public interface IMessageMonitor
    {
        void HandleException(Type messageType);
        void HandleTime(TimeSpan handleTime);
        void IssuePublishingMessage();
        void IncrementThrottlingStatistic();
        void HandleThrottlingTime(TimeSpan handleTime);
        void PublishMessageTime(TimeSpan handleTime);
        void ReceiveMessageTime(TimeSpan handleTime, string queueName, string region);
    }
}
