using System;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Middleware.Handle;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for configuring instances of <see cref="SqsReadConfiguration"/>. This class cannot be inherited.
    /// </summary>
    public sealed class SqsReadConfigurationBuilder : SqsConfigurationBuilder<SqsReadConfiguration, SqsReadConfigurationBuilder>
    {
        /// <inheritdoc />
        protected override SqsReadConfigurationBuilder Self => this;

        /// <summary>
        /// Gets or sets the topic source account Id to use.
        /// </summary>
        private string TopicSourceAccountId { get; set; }

        private string SubscriptionGroupName { get; set; }

        private Action<HandlerMiddlewareBuilder> MiddlewareConfiguration { get; set; }

        /// <summary>
        /// Configures the read configuration to use a custom subscription group.
        /// By default, each queue has its own subscription group.
        /// </summary>
        /// <param name="subscriptionGroupName">The name of the subscription group that this
        /// configuration should be part of</param>
        /// <returns>The current <see cref="SqsReadConfigurationBuilder"/>.</returns>
        public SqsReadConfigurationBuilder WithSubscriptionGroup(string subscriptionGroupName)
        {
            SubscriptionGroupName = subscriptionGroupName;
            return this;
        }

        public SqsReadConfigurationBuilder WithMiddlewareConfiguration(
            Action<HandlerMiddlewareBuilder> middlewareConfiguration)
        {
            MiddlewareConfiguration = middlewareConfiguration;
            return this;
        }

        /// <summary>
        /// Configures the account Id to use for the topic source.
        /// </summary>
        /// <param name="id">The Id of the AWS account which is the topic's source.</param>
        /// <returns>
        /// The current <see cref="SqsReadConfigurationBuilder"/>.
        /// </returns>
        public SqsReadConfigurationBuilder WithTopicSourceAccount(string id)
        {
            TopicSourceAccountId = id;
            return this;
        }

        /// <summary>
        /// Configures the specified <see cref="SqsReadConfiguration"/>.
        /// </summary>
        /// <param name="config">The configuration to configure.</param>
        internal override void Configure(SqsReadConfiguration config)
        {
            // TODO Which ones should be configurable? All, or just the important ones?
            // config.BaseQueueName = default;
            // config.BaseTopicName = default;
            // config.DeliveryDelay = default;
            // config.ErrorQueueRetentionPeriod = default;
            // config.FilterPolicy = default;
            // config.MessageBackoffStrategy = default;
            // config.MessageProcessingStrategy = default;
            // config.PublishEndpoint = default;
            // config.QueueName = default;
            // config.RetryCountBeforeSendingToErrorQueue = default;
            // config.ServerSideEncryption = default;
            // config.Topic = default;

            base.Configure(config);

            if (TopicSourceAccountId != null)
            {
                config.TopicSourceAccount = TopicSourceAccountId;
            }

            if (SubscriptionGroupName != null)
            {
                config.SubscriptionGroupName = SubscriptionGroupName;
            }

            if (MiddlewareConfiguration != null)
            {
                config.MiddlewareConfiguration = MiddlewareConfiguration;
            }
        }
    }
}
