using System;
using Amazon.SQS.Model;

namespace JustSaying.Messaging.Monitoring
{
    public interface IMessageMonitor
    {
        void HandleException(Type messageType);
        void HandleError(Exception ex, Message message);
        void HandleTime(TimeSpan duration);
        void IssuePublishingMessage();
        void Handled(JustSaying.Models.Message message);
        void IncrementThrottlingStatistic();
        void HandleThrottlingTime(TimeSpan duration);
        void PublishMessageTime(TimeSpan duration);
        void ReceiveMessageTime(TimeSpan duration, string queueName, string region);
    }
}
