using System;
using System.Configuration;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.AwsTools.QueueCreation
{
    public class SqsReadConfiguration : SqsBasicConfiguration
    {
        public SqsReadConfiguration()
        {
            VisibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DEFAULT_HANDLER_RETRY_COUNT;
            MessageRetentionSeconds = JustSayingConstants.DEFAULT_RETENTION_PERIOD;
            ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD;
        }

        internal string QueueName { get; set; }
        internal string Topic { get; set; }
        internal string PublishEndpoint { get; set; }

        public int? InstancePosition { get; set; }
        public int? MaxAllowedMessagesInFlight { get; set; }
        public IMessageProcessingStrategy MessageProcessingStrategy { get; set; }
        public Action<Exception, Amazon.SQS.Model.Message> OnError { get; set; }
        public int ErrorQueueRetentionPeriodSeconds { get; set; }

        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(Topic))
                throw new ConfigurationErrorsException("Invalid configuration. Topic must be provided.");
            
            if (MessageRetentionSeconds < JustSayingConstants.MINIMUM_RETENTION_PERIOD || MessageRetentionSeconds > JustSayingConstants.MAXIMUM_RETENTION_PERIOD)
                throw new ConfigurationErrorsException(string.Format("Invalid configuration. MessageRetentionSeconds must be between {0} and {1}.", JustSayingConstants.MINIMUM_RETENTION_PERIOD, JustSayingConstants.MAXIMUM_RETENTION_PERIOD));

            if (ErrorQueueRetentionPeriodSeconds < JustSayingConstants.MINIMUM_RETENTION_PERIOD || ErrorQueueRetentionPeriodSeconds > JustSayingConstants.MAXIMUM_RETENTION_PERIOD)
                throw new ConfigurationErrorsException(string.Format("Invalid configuration. ErrorQueueRetentionPeriodSeconds must be between {0} and {1}.", JustSayingConstants.MINIMUM_RETENTION_PERIOD, JustSayingConstants.MAXIMUM_RETENTION_PERIOD));
            
            if (MaxAllowedMessagesInFlight.HasValue && MessageProcessingStrategy != null)
                throw new ConfigurationErrorsException("You have provided both 'maxAllowedMessagesInFlight' and 'messageProcessingStrategy' - these settings are mutually exclusive.");

            if (PublishEndpoint == null)
                throw new ConfigurationErrorsException("You must provide a value for PublishEndpoint.");
        }
    }
}