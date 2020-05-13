using System;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.AwsTools.QueueCreation
{
    public enum SubscriptionType
    {
        ToTopic,
        PointToPoint
    };

    public class SqsReadConfiguration : SqsBasicConfiguration
    {
        public SqsReadConfiguration(SubscriptionType subscriptionType)
        {
            SubscriptionType = subscriptionType;
            MessageRetention = JustSayingConstants.DefaultRetentionPeriod;
            ErrorQueueRetentionPeriod = JustSayingConstants.MaximumRetentionPeriod;
            VisibilityTimeout = JustSayingConstants.DefaultVisibilityTimeout;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DefaultHandlerRetryCount;
            SubscriptionConfig = new SubscriptionConfigBuilder();
        }

        public SubscriptionConfigBuilder SubscriptionConfig { get; private set; }
        public SubscriptionType SubscriptionType { get; private set; }

        public string TopicName { get; set; }
        public string PublishEndpoint { get; set; }

        public string TopicSourceAccount { get; set; }
        public IMessageBackoffStrategy MessageBackoffStrategy { get; set; }
        public string FilterPolicy { get; set; }
        public string SubscriptionGroupName { get; set; }

        protected override void OnValidate()
        {
            if (SubscriptionType == SubscriptionType.ToTopic)
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

            if (string.IsNullOrWhiteSpace(SubscriptionGroupName))
            {
                throw new ConfigurationErrorsException("You must provide a name for the subscription group");
            }
        }

        public SqsReadConfiguration Copy()
        {
            return new SqsReadConfiguration(SubscriptionType)
            {
                SubscriptionConfig = SubscriptionConfig,
                FilterPolicy = FilterPolicy,
                PublishEndpoint = PublishEndpoint,
                TopicName = TopicName,
                MessageBackoffStrategy = MessageBackoffStrategy,
                TopicSourceAccount = TopicSourceAccount,
                DeliveryDelay = DeliveryDelay,
                MessageRetention = MessageRetention,
                SubscriptionGroupName = SubscriptionGroupName,
                QueueName = QueueName,
                VisibilityTimeout = VisibilityTimeout,
                ErrorQueueOptOut = ErrorQueueOptOut,
                ErrorQueueRetentionPeriod = ErrorQueueRetentionPeriod,
                ServerSideEncryption = ServerSideEncryption,
                RetryCountBeforeSendingToErrorQueue = RetryCountBeforeSendingToErrorQueue
            };
        }
    }
}
