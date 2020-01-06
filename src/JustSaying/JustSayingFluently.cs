using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
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
    public class JustSayingFluently : ISubscriberIntoQueue,
        IHaveFulfilledSubscriptionRequirements,
        IHaveFulfilledPublishRequirements,
        IMayWantOptionalSettings,
        IMayWantARegionPicker,
        IAmJustInterrogating
    {
        private readonly ILogger _log;
        private readonly IVerifyAmazonQueues _amazonQueueCreator;
        private readonly IAwsClientFactoryProxy _awsClientFactoryProxy;
        protected internal IAmJustSaying Bus { get; set; }
        private SqsReadConfiguration _subscriptionConfig = new SqsReadConfiguration(SubscriptionType.ToTopic);
        private IMessageSerializationFactory _serializationFactory;
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
        public IHaveFulfilledPublishRequirements WithSnsMessagePublisher<T>() where T : Message
        {
            return WithSnsMessagePublisher<T>(null);
        }

        /// <summary>
        /// Register for publishing messages to SNS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IHaveFulfilledPublishRequirements WithSnsMessagePublisher<T>(Action<SnsWriteConfiguration> configBuilder) where T : Message
        {
            return AddSnsMessagePublisher<T>(configBuilder);
        }

        private IHaveFulfilledPublishRequirements AddSnsMessagePublisher<T>(Action<SnsWriteConfiguration> configBuilder) where T : Message
        {
            _log.LogInformation("Adding SNS publisher for message type '{MessageType}'.",
                typeof(T));

            var snsWriteConfig = new SnsWriteConfiguration();
            configBuilder?.Invoke(snsWriteConfig);

            _subscriptionConfig.TopicName = GetOrUseDefaultTopicName<T>(_subscriptionConfig.TopicName);

            Bus.SerializationRegister.AddSerializer<T>(_serializationFactory.GetSerializer<T>());

            foreach (var region in Bus.Config.Regions)
            {
                // TODO pass region down into topic creation for when we have foreign topics so we can generate the arn
                var eventPublisher = new SnsTopicByName(
                    _subscriptionConfig.TopicName,
                    _awsClientFactoryProxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(region)),
                    Bus.SerializationRegister,
                    _loggerFactory, snsWriteConfig,
                    Bus.Config.MessageSubjectProvider)
                {
                    MessageResponseLogger = Bus.Config.MessageResponseLogger
                };

                CreatePublisher(eventPublisher, snsWriteConfig);

                eventPublisher.EnsurePolicyIsUpdatedAsync(Bus.Config.AdditionalSubscriberAccounts).GetAwaiter().GetResult();

                Bus.AddMessagePublisher<T>(eventPublisher, region);
            }

            _log.LogInformation("Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'.",
                _subscriptionConfig.TopicName, typeof(T));

            return this;
        }

        private static void CreatePublisher(SnsTopicByName eventPublisher, SnsWriteConfiguration snsWriteConfig)
        {
            if (snsWriteConfig.Encryption != null)
            {
                eventPublisher.CreateWithEncryptionAsync(snsWriteConfig.Encryption).GetAwaiter().GetResult();
            }
            else
            {
                eventPublisher.CreateAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Register for publishing messages to SQS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IHaveFulfilledPublishRequirements WithSqsMessagePublisher<T>(Action<SqsWriteConfiguration> configBuilder) where T : Message
        {
            _log.LogInformation("Adding SQS publisher for message type '{MessageType}'.",
                typeof(T));

            var config = new SqsWriteConfiguration();
            configBuilder?.Invoke(config);

            Bus.SerializationRegister.AddSerializer<T>(_serializationFactory.GetSerializer<T>());

            config.QueueName = GetOrUseDefaultQueueName<T>(config.QueueName);

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

        /// <summary>
        /// I'm done setting up. Fire up listening on this baby...
        /// </summary>
        public void StartListening(CancellationToken cancellationToken = default)
        {
            Bus.Start(cancellationToken);
            _log.LogInformation("Started listening for messages");
        }

        /// <summary>
        /// Publish a message to the stack, asynchronously.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        public virtual async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
        {
            if (Bus == null)
            {
                throw new InvalidOperationException("You must register for message publication before publishing a message");
            }

            await Bus.PublishAsync(message, metadata, cancellationToken)
                .ConfigureAwait(false);
        }

        public IMayWantOptionalSettings WithSerializationFactory(IMessageSerializationFactory factory)
        {
            _serializationFactory = factory;
            return this;
        }

        public IMayWantOptionalSettings WithMessageLockStoreOf(IMessageLockAsync messageLock)
        {
            Bus.MessageLock = messageLock;
            return this;
        }

        public IMayWantOptionalSettings WithMessageContextAccessor(IMessageContextAccessor messageContextAccessor)
        {
            Bus.MessageContextAccessor = messageContextAccessor;
            return this;
        }

        public IFluentSubscription ConfigureSubscriptionWith(Action<SqsReadConfiguration> configBuilder)
        {
            configBuilder(_subscriptionConfig);
            return this;
        }

        public ISubscriberIntoQueue WithSqsTopicSubscriber(string topicName = null)
        {
            _subscriptionConfig = new SqsReadConfiguration(SubscriptionType.ToTopic)
            {
                TopicName = (topicName ?? string.Empty).ToLowerInvariant()
            };
            return this;
        }

        public ISubscriberIntoQueue WithSqsPointToPointSubscriber()
        {
            _subscriptionConfig = new SqsReadConfiguration(SubscriptionType.PointToPoint);
            return this;
        }

        public IFluentSubscription IntoQueue(string queueName)
        {
            _subscriptionConfig.QueueName = queueName;
            return this;
        }

        public IHaveFulfilledSubscriptionRequirements WithMessageHandler<T>(IHandlerResolver handlerResolver) where T : Message
        {
            if (_serializationFactory == null)
            {
                throw new InvalidOperationException($"No {nameof(IMessageSerializationFactory)} has been configured.");
            }

            _subscriptionConfig.TopicName = GetOrUseDefaultTopicName<T>(_subscriptionConfig.TopicName);
            _subscriptionConfig.QueueName = GetOrUseDefaultQueueName<T>(_subscriptionConfig.QueueName);

            var thing = _subscriptionConfig.SubscriptionType == SubscriptionType.PointToPoint
                ? PointToPointHandler<T>()
                : TopicHandler<T>();

            Bus.SerializationRegister.AddSerializer<T>(_serializationFactory.GetSerializer<T>());

            var resolutionContext = new HandlerResolutionContext(_subscriptionConfig.QueueName);
            var proposedHandler = handlerResolver.ResolveHandler<T>(resolutionContext);

            if (proposedHandler == null)
            {
                throw new HandlerNotRegisteredWithContainerException($"There is no handler for '{typeof(T)}' messages.");
            }

            foreach (var region in Bus.Config.Regions)
            {
                Bus.AddMessageHandler(region, _subscriptionConfig.QueueName, () => handlerResolver.ResolveHandler<T>(resolutionContext));
            }

            _log.LogInformation(
                "Added a message handler for message type for '{MessageType}' on topic '{TopicName}' and queue '{QueueName}'.",
                typeof(T),
                _subscriptionConfig.TopicName,
                _subscriptionConfig.QueueName);

            return thing;
        }

        private IHaveFulfilledSubscriptionRequirements TopicHandler<T>() where T : Message
        {
            ConfigureSqsSubscriptionViaTopic();

            foreach (string region in Bus.Config.Regions)
            {
                // TODO Make this async and remove GetAwaiter().GetResult() call
                var queue = _amazonQueueCreator.EnsureTopicExistsWithQueueSubscribedAsync(
                    region, Bus.SerializationRegister,
                    _subscriptionConfig,
                    Bus.Config.MessageSubjectProvider).GetAwaiter().GetResult();

                CreateSubscriptionListener<T>(region, queue);

                _log.LogInformation(
                    "Created SQS topic subscription on topic '{TopicName}' and queue '{QueueName}'.",
                    _subscriptionConfig.TopicName,
                    _subscriptionConfig.QueueName);
            }

            return this;
        }

        private IHaveFulfilledSubscriptionRequirements PointToPointHandler<T>() where T : Message
        {
            ConfigureSqsSubscription<T>();

            foreach (var region in Bus.Config.Regions)
            {
                // TODO Make this async and remove GetAwaiter().GetResult() call
                var queue = _amazonQueueCreator.EnsureQueueExistsAsync(region, _subscriptionConfig).GetAwaiter().GetResult();

                CreateSubscriptionListener<T>(region, queue);

                _log.LogInformation(
                    "Created SQS subscriber for message type '{MessageType}' on queue '{QueueName}'.",
                    typeof(T),
                    _subscriptionConfig.QueueName);
            }

            return this;
        }

        protected INotificationSubscriber CreateSubscriber(SqsQueueBase queue)
        {
            return new SqsNotificationListener(
                queue,
                Bus.SerializationRegister,
                Bus.Monitor,
                _loggerFactory,
                Bus.MessageContextAccessor,
                _subscriptionConfig.OnError,
                Bus.MessageLock,
                _subscriptionConfig.MessageBackoffStrategy);
        }

        private void CreateSubscriptionListener<T>(string region, SqsQueueBase queue)
            where T : Message
        {
            INotificationSubscriber subscriber = CreateSubscriber(queue);

            subscriber.Subscribers.Add(new Subscriber(typeof(T)));

            Bus.AddNotificationSubscriber(region, subscriber);

            // TODO Concrete type check for backwards compatibility for now.
            // Refactor the interface for v7 to allow this to be done against the interface.
            if (subscriber is SqsNotificationListener sqsSubscriptionListener)
            {
                if (_subscriptionConfig.MaxAllowedMessagesInFlight.HasValue)
                {
                    sqsSubscriptionListener.WithMaximumConcurrentLimitOnMessagesInFlightOf(_subscriptionConfig.MaxAllowedMessagesInFlight.Value);
                }

                if (_subscriptionConfig.MessageProcessingStrategy != null)
                {
                    sqsSubscriptionListener.WithMessageProcessingStrategy(_subscriptionConfig.MessageProcessingStrategy);
                }
            }
        }

        private void ConfigureSqsSubscriptionViaTopic()
        {
            _subscriptionConfig.PublishEndpoint = _subscriptionConfig.TopicName;
            _subscriptionConfig.TopicName = _subscriptionConfig.TopicName;
            _subscriptionConfig.QueueName = _subscriptionConfig.QueueName;

            _subscriptionConfig.Validate();
        }

        private void ConfigureSqsSubscription<T>() where T : Message
        {
            _subscriptionConfig.ValidateSqsConfiguration();
            _subscriptionConfig.QueueName = _subscriptionConfig.QueueName;
        }

        /// <summary>
        /// Provide your own monitoring implementation
        /// </summary>
        /// <param name="messageMonitor">Monitoring class to be used</param>
        /// <returns></returns>
        public IMayWantOptionalSettings WithMonitoring(IMessageMonitor messageMonitor)
        {
            Bus.Monitor = messageMonitor;
            return this;
        }

        public IHaveFulfilledPublishRequirements ConfigurePublisherWith(Action<IPublishConfiguration> confBuilder)
        {
            confBuilder(Bus.Config);
            Bus.Config.Validate();

            return this;
        }

        public IMayWantARegionPicker WithFailoverRegion(string region)
        {
            Bus.Config.Regions.Add(region);
            return this;
        }

        public IMayWantARegionPicker WithFailoverRegions(params string[] regions)
        {
            foreach (var region in regions)
            {
                Bus.Config.Regions.Add(region);
            }
            Bus.Config.Validate();
            return this;
        }

        public IMayWantOptionalSettings WithActiveRegion(Func<string> getActiveRegion)
        {
            Bus.Config.GetActiveRegion = getActiveRegion;
            return this;
        }

        public IInterrogationResponse WhatDoIHave()
        {
            return (Bus as IAmJustInterrogating)?.WhatDoIHave();
        }

        public IMayWantOptionalSettings WithAwsClientFactory(Func<IAwsClientFactory> awsClientFactory)
        {
            _awsClientFactoryProxy.SetAwsClientFactory(awsClientFactory);
            return this;
        }

        private  string GetOrUseDefaultTopicName<T>(string topicName)
        {
            return string.IsNullOrWhiteSpace(topicName) ? Bus.Config.DefaultTopicNamingConvention.TopicName<T>() : topicName;
        }

        private string GetOrUseDefaultQueueName<T>(string queueName)
        {
            return string.IsNullOrWhiteSpace(queueName) ? Bus.Config.DefaultQueueNamingConvention.QueueName<T>() : queueName;
        }
    }
}
