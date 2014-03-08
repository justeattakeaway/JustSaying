using System;
using System.Configuration;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.AwsTools.MessageProcessingStrategies;

namespace JustEat.Simples.NotificationStack.Stack.Amazon
{
    public class SqsConfiguration
    {
        public SqsConfiguration()
        {
            VisibilityTimeoutSeconds = NotificationStackConstants.DEFAULT_VISIBILITY_TIMEOUT;
            RetryCountBeforeSendingToErrorQueue = 5;
        }
        public string QueueName { get; set; }
        public string Topic { get; set; }
        public int MessageRetentionSeconds { get; set; }
        public int VisibilityTimeoutSeconds { get; set; }
        public int? InstancePosition { get; set; }
        public bool ErrorQueueOptOut { get; set; }
        public int RetryCountBeforeSendingToErrorQueue { get; set; }
        public int? MaxAllowedMessagesInFlight { get; set; }
        public IMessageProcessingStrategy MessageProcessingStrategy { get; set; }
        public Action<Exception> OnError { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Topic))
                throw new ConfigurationErrorsException("Invalid configuration. Topic must be provided.");
            
            if (MessageRetentionSeconds < NotificationStackConstants.MINIMUM_RETENTION_PERIOD || MessageRetentionSeconds > NotificationStackConstants.MAXIMUM_RETENTION_PERIOD)
                throw new ConfigurationErrorsException(string.Format("Invalid configuration. MessageRetentionSeconds must be between {0} and {1}.", NotificationStackConstants.MINIMUM_RETENTION_PERIOD, NotificationStackConstants.MAXIMUM_RETENTION_PERIOD));
            
            if (MaxAllowedMessagesInFlight.HasValue && MessageProcessingStrategy != null)
                throw new ConfigurationErrorsException("You have provided both 'maxAllowedMessagesInFlight' and 'messageProcessingStrategy' - these settings are mutually exclusive.");
        }
    }
}