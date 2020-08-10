using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;

namespace JustSaying
{
    /// <summary>
    /// Fluently configure a JustSaying message bus.
    /// Intended usage:
    /// 1. Factory.JustSaying(); // Gimme a bus
    /// 2. WithMonitoring(instance) // Ensure you monitor the messaging status
    /// 3. Set subscribers - WithSqsTopicSubscriber() / WithSnsTopicSubscriber() etc // ToDo: Shouldn't be enforced in base! Is a JE concern.
    /// 3. Set Handlers - WithTopicMessageHandler()
    /// </summary>
    public class JustSayingFluently
    {
        private readonly ILogger _log;
        private readonly IVerifyAmazonQueues _amazonQueueCreator;
        private readonly IAwsClientFactoryProxy _awsClientFactoryProxy;
        protected internal IAmJustSaying Bus { get; set; }
        private readonly ILoggerFactory _loggerFactory;

        protected internal JustSayingFluently(
            IAmJustSaying bus,
            IVerifyAmazonQueues queueCreator,
            IAwsClientFactoryProxy awsClientFactoryProxy,
            ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _log = _loggerFactory.CreateLogger("JustSaying");
            Bus = bus;
            _amazonQueueCreator = queueCreator;
            _awsClientFactoryProxy = awsClientFactoryProxy;
        }


        /// <summary>
        /// Register for publishing messages to SNS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public JustSayingFluently WithSnsMessagePublisher<T>(SqsReadConfiguration subscriptionConfig, Action<SnsWriteConfiguration> configBuilder) where T : Message
        {
            _log.LogInformation("Adding SNS publisher for message type '{MessageType}'.",
                typeof(T));

            var snsWriteConfig = new SnsWriteConfiguration();
            configBuilder?.Invoke(snsWriteConfig);

            subscriptionConfig.ApplyTopicNamingConvention<T>(Bus.Config.TopicNamingConvention);

            Bus.SerializationRegister.AddSerializer<T>();

            foreach (var region in Bus.Config.Regions)
            {
                // TODO pass region down into topic creation for when we have foreign topics so we can generate the arn
                var eventPublisher = new SnsTopicByName(
                    subscriptionConfig.TopicName,
                    _awsClientFactoryProxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(region)),
                    Bus.SerializationRegister,
                    _loggerFactory, snsWriteConfig,
                    Bus.Config.MessageSubjectProvider)
                {
                    MessageResponseLogger = Bus.Config.MessageResponseLogger
                };

                if (snsWriteConfig.Encryption != null)
                {
                    eventPublisher.CreateWithEncryptionAsync(snsWriteConfig.Encryption).GetAwaiter().GetResult();
                }
                else
                {
                    eventPublisher.CreateAsync().GetAwaiter().GetResult();
                }

                eventPublisher.EnsurePolicyIsUpdatedAsync(Bus.Config.AdditionalSubscriberAccounts).GetAwaiter().GetResult();

                Bus.AddMessagePublisher<T>(eventPublisher, region);
            }

            _log.LogInformation("Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'.",
                subscriptionConfig.TopicName, typeof(T));

            return this;
        }

        /// <summary>
        /// Register for publishing messages to SQS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public JustSayingFluently WithSqsMessagePublisher<T>(Action<SqsWriteConfiguration> configBuilder) where T : Message
        {
            _log.LogInformation("Adding SQS publisher for message type '{MessageType}'.",
                typeof(T));

            var config = new SqsWriteConfiguration();
            configBuilder?.Invoke(config);

            config.ApplyQueueNamingConvention<T>(Bus.Config.QueueNamingConvention);

            foreach (var region in Bus.Config.Regions)
            {
                var regionEndpoint = RegionEndpoint.GetBySystemName(region);
                var sqsClient = _awsClientFactoryProxy.GetAwsClientFactory().GetSqsClient(regionEndpoint);

                var eventPublisher = new SqsPublisher(
                    regionEndpoint,
                    config.QueueName,
                    sqsClient,
                    config.RetryCountBeforeSendingToErrorQueue,
                    Bus.SerializationRegister,
                    _loggerFactory)
                {
                    MessageResponseLogger = Bus.Config.MessageResponseLogger
                };

                if (!eventPublisher.ExistsAsync().GetAwaiter().GetResult())
                {
                    eventPublisher.CreateAsync(config).GetAwaiter().GetResult();
                }

                Bus.AddMessagePublisher<T>(eventPublisher, region);
            }

            _log.LogInformation(
                "Created SQS publisher for message type '{MessageType}' on queue '{QueueName}'.",
                typeof(T),
                config.QueueName);

            return this;
        }

        public void WithMessageHandler<T>(
            SqsReadConfiguration subscriptionConfig,
            IHandlerResolver handlerResolver)
            where T : Message
        {
            if (handlerResolver is null) throw new ArgumentNullException(nameof(handlerResolver));
            if (subscriptionConfig is null) throw new ArgumentNullException(nameof(subscriptionConfig));

            subscriptionConfig.ApplyTopicNamingConvention<T>(Bus.Config.TopicNamingConvention);
            subscriptionConfig.ApplyQueueNamingConvention<T>(Bus.Config.QueueNamingConvention);

            subscriptionConfig.SubscriptionGroupName ??= subscriptionConfig.QueueName;

            if (subscriptionConfig.SubscriptionType == SubscriptionType.PointToPoint)
            {
                foreach (var region in Bus.Config.Regions)
                {
                    // TODO Make this async and remove GetAwaiter().GetResult() call
                    var queue = _amazonQueueCreator.EnsureQueueExistsAsync(region, subscriptionConfig)
                        .GetAwaiter().GetResult();

                    Bus.AddQueue(region, subscriptionConfig.SubscriptionGroupName, queue);

                    _log.LogInformation(
                        "Created SQS subscriber for message type '{MessageType}' on queue '{QueueName}'.",
                        typeof(T),
                        subscriptionConfig.QueueName);
                }
            }
            else if(subscriptionConfig.SubscriptionType == SubscriptionType.ToTopic)
            {
                subscriptionConfig.PublishEndpoint = subscriptionConfig.TopicName;
                subscriptionConfig.Validate();

                foreach (string region in Bus.Config.Regions)
                {
                    // TODO Make this async and remove GetAwaiter().GetResult() call
                    var queue = _amazonQueueCreator.EnsureTopicExistsWithQueueSubscribedAsync(
                        region, Bus.SerializationRegister,
                        subscriptionConfig,
                        Bus.Config.MessageSubjectProvider).GetAwaiter().GetResult();

                    Bus.AddQueue(region,  subscriptionConfig.SubscriptionGroupName, queue);

                    _log.LogInformation(
                        "Created SQS topic subscription on topic '{TopicName}' and queue '{QueueName}'.",
                        subscriptionConfig.TopicName,
                        subscriptionConfig.QueueName);
                }
            }

            var resolutionContext = new HandlerResolutionContext(subscriptionConfig.QueueName);
            var proposedHandler = handlerResolver.ResolveHandler<T>(resolutionContext);

            if (proposedHandler == null)
            {
                throw new HandlerNotRegisteredWithContainerException($"There is no handler for '{typeof(T)}' messages.");
            }

            Bus.AddMessageHandler(subscriptionConfig.QueueName, () => handlerResolver.ResolveHandler<T>(resolutionContext));

            _log.LogInformation(
                "Added a message handler for message type for '{MessageType}' on topic '{TopicName}' and queue '{QueueName}'.",
                typeof(T),
                subscriptionConfig.TopicName,
                subscriptionConfig.QueueName);
        }

        public InterrogationResult Interrogate()
        {
            return Bus.Interrogate();
        }
    }
}
