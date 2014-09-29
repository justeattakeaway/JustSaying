using System;
using System.Configuration;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.AwsTools.QueueCreation
{
    public enum SubscriptionType { ToTopic, PointToPoint };

    public class SqsReadConfiguration : SqsBasicConfiguration
    {
        
        public SqsReadConfiguration(SubscriptionType subscriptionType)
        {
            SubscriptionType = subscriptionType;
            MessageRetentionSeconds = JustSayingConstants.DEFAULT_RETENTION_PERIOD;
            ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD;
            VisibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DEFAULT_HANDLER_RETRY_COUNT;
        }

        internal SubscriptionType SubscriptionType { get; private set; }
        internal string QueueName { get; set; }
        internal string Topic { get; set; }
        internal string PublishEndpoint { get; set; }

        public int? InstancePosition { get; set; }
        public int? MaxAllowedMessagesInFlight { get; set; }
        public IMessageProcessingStrategy MessageProcessingStrategy { get; set; }
        public Action<Exception, Amazon.SQS.Model.Message> OnError { get; set; }

        public override void Validate()
        {
            ValidateSqsConfiguration();
            ValidateSnsConfiguration();
        }

        public void ValidateSqsConfiguration()
        {
            base.Validate();

            if (MaxAllowedMessagesInFlight.HasValue && MessageProcessingStrategy != null)
                throw new ConfigurationErrorsException("You have provided both 'maxAllowedMessagesInFlight' and 'messageProcessingStrategy' - these settings are mutually exclusive.");
        }

        private void ValidateSnsConfiguration()
        {
            if (string.IsNullOrWhiteSpace(Topic))
                throw new ConfigurationErrorsException("Invalid configuration. Topic must be provided.");

            if (PublishEndpoint == null)
                throw new ConfigurationErrorsException("You must provide a value for PublishEndpoint.");
        }
    }
}