using System;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.v2.Configuration
{
    public interface IAwsSubscriberConfiguration
    {
        string QueueNameOverride { get; set; }
        Action<Exception, Message> OnError { get; set; }
        IMessageProcessingStrategy MessageProcessingStrategy { get; set; }
        IMessageBackoffStrategy MessageBackoffStrategy { get; set; }
        int? MaxAllowedMessagesInFlight { get; set; }
        int MessageRetentionSeconds { get; set; }
        int ErrorQueueRetentionPeriodSeconds { get; set; }
        int VisibilityTimeoutSeconds { get; set; }
        int DeliveryDelaySeconds { get; set; }
        int RetryCountBeforeSendingToErrorQueue { get; set; }
        bool ErrorQueueOptOut { get; set; }
    }

    public abstract class AwsSubscriberConfiguration : IAwsSubscriberConfiguration
    {
        public string QueueNameOverride { get; set; }
        public Action<Exception, Message> OnError { get; set; }
        public IMessageProcessingStrategy MessageProcessingStrategy { get; set; }
        public IMessageBackoffStrategy MessageBackoffStrategy { get; set; }
        public int? MaxAllowedMessagesInFlight { get; set; }
        public int MessageRetentionSeconds { get; set; }
        public int ErrorQueueRetentionPeriodSeconds { get; set; }
        public int VisibilityTimeoutSeconds { get; set; }
        public int DeliveryDelaySeconds { get; set; }
        public int RetryCountBeforeSendingToErrorQueue { get; set; }
        public bool ErrorQueueOptOut { get; set; }
    }
}