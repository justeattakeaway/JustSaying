using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
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
        IAmJustInterrogating,
        IMayWantMessageLockStore
    {
        private readonly ILogger _log;
        private readonly IVerifyAmazonQueues _amazonQueueCreator;
        private readonly IAwsClientFactoryProxy _awsClientFactoryProxy;
        protected internal IAmJustSaying Bus { get; set; }
        private SqsReadConfiguration _subscriptionConfig = new SqsReadConfiguration(SubscriptionType.ToTopic);
        private IMessageSerializationFactory _serializationFactory;
        private Func<INamingStrategy> _busNamingStrategyFunc;
        private readonly ILoggerFactory _loggerFactory;

        protected internal JustSayingFluently(IAmJustSaying bus, IVerifyAmazonQueues queueCreator, IAwsClientFactoryProxy awsClientFactoryProxy, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _log = _loggerFactory.CreateLogger("JustSaying");
            Bus = bus;
            _amazonQueueCreator = queueCreator;
            _awsClientFactoryProxy = awsClientFactoryProxy;
        }

        public virtual INamingStrategy GetNamingStrategy()
            => _busNamingStrategyFunc != null
                ? _busNamingStrategyFunc()
                : new DefaultNamingStrategy();

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

            _subscriptionConfig.Topic = typeof(T).ToTopicName();
            var namingStrategy = GetNamingStrategy();

            Bus.SerializationRegister.AddSerializer<T>(_serializationFactory.GetSerializer<T>());

            var topicName = namingStrategy.GetTopicName(_subscriptionConfig.BaseTopicName, typeof(T));
            foreach (var region in Bus.Config.Regions)
            {
                // TODO pass region down into topic creation for when we have foreign topics so we can generate the arn
                var eventPublisher = new SnsTopicByName(
                    topicName,
                    _awsClientFactoryProxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(region)),
                    Bus.SerializationRegister,
                    _loggerFactory, snsWriteConfig,
                    Bus.Config.MessageSubjectProvider)
                {
                    MessageResponseLogger = Bus.Config.MessageResponseLogger
                };

                CreatePublisher<T>(eventPublisher, snsWriteConfig);

                eventPublisher.EnsurePolicyIsUpdatedAsync(Bus.Config.AdditionalSubscriberAccounts).GetAwaiter().GetResult();

                Bus.AddMessagePublisher<T>(eventPublisher, region);
            }

            _log.LogInformation("Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'.",
                _subscriptionConfig.Topic, typeof(T));

            return this;
        }

        private void CreatePublisher<T>(SnsTopicByName eventPublisher, SnsWriteConfiguration snsWriteConfig) where T : Message
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

            var queueName = GetNamingStrategy().GetQueueName(new SqsReadConfiguration(SubscriptionType.PointToPoint) { BaseQueueName = config.QueueName }, typeof(T));

            Bus.SerializationRegister.AddSerializer<T>(_serializationFactory.GetSerializer<T>());

            foreach (var region in Bus.Config.Regions)
            {
                var regionEndpoint = RegionEndpoint.GetBySystemName(region);
                var sqsClient = _awsClientFactoryProxy.GetAwsClientFactory().GetSqsClient(regionEndpoint);

                var eventPublisher = new SqsPublisher(
                    regionEndpoint,
                    queueName,
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

            _log.LogInformation("Created SQS publisher for message type '{MessageType}' on queue '{QueueName}'.",
                typeof(T), queueName);

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
                BaseTopicName = (topicName ?? string.Empty).ToLowerInvariant()
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
            _subscriptionConfig.BaseQueueName = queueName;
            return this;
        }

        /// <summary>
        /// Set message handlers for the given topic
        /// </summary>
        /// <typeparam name="T">Message type to be handled</typeparam>
        /// <param name="handler">Handler for the message type</param>
        /// <returns></returns>
        public IHaveFulfilledSubscriptionRequirements WithMessageHandler<T>(IHandlerAsync<T> handler) where T : Message
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (_serializationFactory == null)
            {
                throw new InvalidOperationException($"No {nameof(IMessageSerializationFactory)} has been configured.");
            }

            // TODO - Subscription listeners should be just added once per queue,
            // and not for each message handler
            var thing = _subscriptionConfig.SubscriptionType == SubscriptionType.PointToPoint
                ? PointToPointHandler<T>()
                : TopicHandler<T>();

            Bus.SerializationRegister.AddSerializer<T>(_serializationFactory.GetSerializer<T>());
            foreach (var region in Bus.Config.Regions)
            {
                Bus.AddMessageHandler(region, _subscriptionConfig.QueueName, () => handler);
            }
            _log.LogInformation("Added a message handler of type '{HandlerType}' for message type '{MessageType}' on queue '{QueueName}'.",
                handler.GetType(), typeof(T), _subscriptionConfig.QueueName);

            return thing;
        }

        public IHaveFulfilledSubscriptionRequirements WithMessageHandler<T>(IHandlerResolver handlerResolver) where T : Message
        {
            if (_serializationFactory == null)
            {
                throw new InvalidOperationException($"No {nameof(IMessageSerializationFactory)} has been configured.");
            }

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

            _log.LogInformation("Added a message handler for message type for '{MessageType}' on topic '{TopicName}' and queue '{QueueName}'.",
                typeof(T), _subscriptionConfig.Topic, _subscriptionConfig.QueueName);

            return thing;
        }

        private IHaveFulfilledSubscriptionRequirements TopicHandler<T>() where T : Message
        {
            ConfigureSqsSubscriptionViaTopic<T>();

            foreach (var region in Bus.Config.Regions)
            {
                var queue = _amazonQueueCreator.EnsureTopicExistsWithQueueSubscribedAsync(region, Bus.SerializationRegister, _subscriptionConfig, Bus.Config.MessageSubjectProvider).GetAwaiter().GetResult();
                CreateSubscriptionListener<T>(region, queue);
                _log.LogInformation("Created SQS topic subscription on topic '{TopicName}' and queue '{QueueName}'.",
                    _subscriptionConfig.Topic, _subscriptionConfig.QueueName);
            }

            return this;
        }

        private IHaveFulfilledSubscriptionRequirements PointToPointHandler<T>() where T : Message
        {
            ConfigureSqsSubscription<T>();

            foreach (var region in Bus.Config.Regions)
            {
                var queue = _amazonQueueCreator.EnsureQueueExistsAsync(region, _subscriptionConfig).GetAwaiter().GetResult();
                CreateSubscriptionListener<T>(region, queue);
                _log.LogInformation("Created SQS subscriber for message type '{MessageType}' on queue '{QueueName}'.",
                    typeof(T), _subscriptionConfig.QueueName);
            }

            return this;
        }

        private void CreateSubscriptionListener<T>(string region, SqsQueueBase queue) where T : Message
        {
            var sqsSubscriptionListener = new SqsNotificationListener(
                queue, Bus.SerializationRegister, Bus.Monitor, _loggerFactory,
                Bus.MessageContextAccessor,
                _subscriptionConfig.OnError, Bus.MessageLock,
                _subscriptionConfig.MessageBackoffStrategy);

            sqsSubscriptionListener.Subscribers.Add(new Subscriber(typeof(T)));
            Bus.AddNotificationSubscriber(region, sqsSubscriptionListener);

            if (_subscriptionConfig.MaxAllowedMessagesInFlight.HasValue)
            {
                sqsSubscriptionListener.WithMaximumConcurrentLimitOnMessagesInFlightOf(_subscriptionConfig.MaxAllowedMessagesInFlight.Value);
            }

            if (_subscriptionConfig.MessageProcessingStrategy != null)
            {
                sqsSubscriptionListener.WithMessageProcessingStrategy(_subscriptionConfig.MessageProcessingStrategy);
            }
        }

        private void ConfigureSqsSubscriptionViaTopic<T>() where T : Message
        {
            var namingStrategy = GetNamingStrategy();
            _subscriptionConfig.PublishEndpoint = namingStrategy.GetTopicName(_subscriptionConfig.BaseTopicName, typeof(T));
            _subscriptionConfig.Topic = namingStrategy.GetTopicName(_subscriptionConfig.BaseTopicName, typeof(T));
            _subscriptionConfig.QueueName = namingStrategy.GetQueueName(_subscriptionConfig, typeof(T));
            _subscriptionConfig.Validate();
        }

        private void ConfigureSqsSubscription<T>() where T : Message
        {
            _subscriptionConfig.ValidateSqsConfiguration();
            _subscriptionConfig.QueueName = GetNamingStrategy().GetQueueName(_subscriptionConfig, typeof(T));
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

        public IMayWantOptionalSettings WithNamingStrategy(Func<INamingStrategy> busNamingStrategy)
        {
            _busNamingStrategyFunc = busNamingStrategy;
            return this;
        }

        public IMayWantOptionalSettings WithAwsClientFactory(Func<IAwsClientFactory> awsClientFactory)
        {
            _awsClientFactoryProxy.SetAwsClientFactory(awsClientFactory);
            return this;
        }
    }
}
