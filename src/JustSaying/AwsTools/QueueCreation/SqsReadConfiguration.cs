using System;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.AwsTools.QueueCreation
{
    public enum SubscriptionType { ToTopic, PointToPoint };

    public class SqsReadConfiguration : SqsBasicConfiguration
    {
        public SqsReadConfiguration(SubscriptionType subscriptionType)
        {
            SubscriptionType = subscriptionType;
            MessageRetention = JustSayingConstants.DefaultRetentionPeriod;
            ErrorQueueRetentionPeriod = JustSayingConstants.MaximumRetentionPeriod;
            VisibilityTimeout = JustSayingConstants.DefaultVisibilityTimeout;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DefaultHandlerRetryCount;
        }

        public SubscriptionType SubscriptionType { get; private set; }

        public string QueueName { get; set; }
        public string TopicName { get; set; }
        public string PublishEndpoint { get; set; }

        public int? MaxAllowedMessagesInFlight { get; set; }
        public IMessageProcessingStrategy MessageProcessingStrategy { get; set; }
        public Action<Exception, Amazon.SQS.Model.Message> OnError { get; set; }
        public string TopicSourceAccount { get; set; }
        public IMessageBackoffStrategy MessageBackoffStrategy { get; set; }
        public string FilterPolicy { get; set; }

        public override void Validate()
        {
            ValidateSqsConfiguration();
            ValidateSnsConfiguration();
        }

        public void ValidateSqsConfiguration()
        {
            base.Validate();

            if (MaxAllowedMessagesInFlight.HasValue && MessageProcessingStrategy != null)
            {
                throw new ConfigurationErrorsException("You have provided both 'maxAllowedMessagesInFlight' and 'messageProcessingStrategy' - these settings are mutually exclusive.");
            }
        }

        private void ValidateSnsConfiguration()
        {
            if (string.IsNullOrWhiteSpace(TopicName))
            {
                throw new ConfigurationErrorsException("Invalid configuration. Topic name must be provided.");
            }

            if (PublishEndpoint == null)
            {
                throw new ConfigurationErrorsException("You must provide a value for PublishEndpoint.");
            }
        }
    }
}
