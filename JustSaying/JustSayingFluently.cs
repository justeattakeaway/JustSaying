using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using JustSaying.Messaging.Interrogation;
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
    public class JustSayingFluently : ISubscriberIntoQueue, IHaveFulfilledSubscriptionRequirements, IHaveFulfilledPublishRequirements, IMayWantOptionalSettings, IMayWantARegionPicker, IAmJustInterrogating
    {
        private readonly ILogger _log;
        private readonly IVerifyAmazonQueues _amazonQueueCreator;
        private readonly IAwsClientFactoryProxy _awsClientFactoryProxy;
        protected readonly IAmJustSaying Bus;
        private SqsReadConfiguration _subscriptionConfig = new SqsReadConfiguration(SubscriptionType.ToTopic);
        private IMessageSerialisationFactory _serialisationFactory;
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

        private static string GetMessageTypeName<T>() => typeof(T).ToTopicName();

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
            _log.LogInformation("Adding SNS publisher");

            var snsWriteConfig = new SnsWriteConfiguration();
            configBuilder?.Invoke(snsWriteConfig);

            _subscriptionConfig.Topic = GetMessageTypeName<T>();
            var namingStrategy = GetNamingStrategy();

            Bus.SerialisationRegister.AddSerialiser<T>(_serialisationFactory.GetSerialiser<T>());

            var topicName = namingStrategy.GetTopicName(_subscriptionConfig.BaseTopicName, GetMessageTypeName<T>());
            foreach (var region in Bus.Config.Regions)
            {
                // TODO pass region down into topic creation for when we have foreign topics so we can generate the arn
                var eventPublisher = new SnsTopicByName(
                    topicName,
                    _awsClientFactoryProxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(region)),
                    Bus.SerialisationRegister,
                    _loggerFactory, snsWriteConfig)
                {
                    MessageResponseLogger = Bus.Config.MessageResponseLogger
                };

                eventPublisher.CreateAsync().GetAwaiter().GetResult();

                eventPublisher.EnsurePolicyIsUpdatedAsync(Bus.Config.AdditionalSubscriberAccounts).GetAwaiter().GetResult();

                Bus.AddMessagePublisher<T>(eventPublisher, region);
            }

            _log.LogInformation($"Created SNS topic publisher - Topic: {_subscriptionConfig.Topic}");

            return this;
        }

        /// <summary>
        /// Register for publishing messages to SQS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IHaveFulfilledPublishRequirements WithSqsMessagePublisher<T>(Action<SqsWriteConfiguration> configBuilder) where T : Message
        {
            _log.LogInformation("Adding SQS publisher");

            var config = new SqsWriteConfiguration();
            configBuilder(config);

            var messageTypeName = typeof(T).ToTopicName();
            var queueName = GetNamingStrategy().GetQueueName(new SqsReadConfiguration(SubscriptionType.PointToPoint){BaseQueueName = config.QueueName}, messageTypeName);

            Bus.SerialisationRegister.AddSerialiser<T>(_serialisationFactory.GetSerialiser<T>());

            foreach (var region in Bus.Config.Regions)
            {
                var regionEndpoint = RegionEndpoint.GetBySystemName(region);
                var eventPublisher = new SqsPublisher(
                    regionEndpoint,
                    queueName,
                    _awsClientFactoryProxy.GetAwsClientFactory().GetSqsClient(regionEndpoint),
                    config.RetryCountBeforeSendingToErrorQueue,
                    Bus.SerialisationRegister,
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

            _log.LogInformation($"Created SQS publisher - MessageName: {messageTypeName}, QueueName: {queueName}");

            return this;
        }

        /// <summary>
        /// I'm done setting up. Fire up listening on this baby...
        /// </summary>
        public void StartListening()
        {
            Bus.Start();
            _log.LogInformation("Started listening for messages");
        }

        /// <summary>
        /// Gor graceful shutdown of all listening threads
        /// </summary>
        public void StopListening()
        {
            Bus.Stop();
            _log.LogInformation("Stopped listening for messages");
        }

#if AWS_SDK_HAS_SYNC
        /// <summary>
        /// Publish a message to the stack.
        /// </summary>
        /// <param name="message"></param>
        public virtual void Publish(Message message)
        {
            if (Bus == null)
            {
                throw new InvalidOperationException("You must register for message publication before publishing a message");
            }

            Bus.Publish(message);
        }
#endif

        /// <summary>
        /// Publish a message to the stack, asynchronously.
        /// </summary>
        /// <param name="message"></param>
        public virtual Task PublishAsync(Message message) => PublishAsync(message, CancellationToken.None);

        /// <summary>
        /// Publish a message to the stack, asynchronously.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        public virtual async Task PublishAsync(Message message, CancellationToken cancellationToken)
        {
            if (Bus == null)
            {
                throw new InvalidOperationException("You must register for message publication before publishing a message");
            }

            await Bus.PublishAsync(message, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// States whether the stack is listening for messages (subscriptions are running)
        /// </summary>
        public bool Listening => Bus?.Listening == true;

        public IMayWantOptionalSettings WithSerialisationFactory(IMessageSerialisationFactory factory)
        {
            _serialisationFactory = factory;
            return this;
        }

        public IMayWantOptionalSettings WithMessageLockStoreOf(IMessageLock messageLock)
        {
            Bus.MessageLock = new BlockingMessageLock(messageLock);
            return this;
        }

        public IMayWantOptionalSettings WithMessageLockStoreOf(IMessageLockAsync messageLock)
        {
            Bus.MessageLock = messageLock;
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
                BaseTopicName = (topicName ?? string.Empty).ToLower()
            };
            return this;
        }

        public ISubscriberIntoQueue WithSqsPointToPointSubscriber()
        {
            _subscriptionConfig = new SqsReadConfiguration(SubscriptionType.PointToPoint);
            return this;
        }

        public IFluentSubscription IntoQueue(string queuename)
        {
            _subscriptionConfig.BaseQueueName = queuename;
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
            // TODO - Subscription listeners should be just added once per queue,
            // and not for each message handler
            var thing =  _subscriptionConfig.SubscriptionType == SubscriptionType.PointToPoint
                ? PointToPointHandler<T>()
                : TopicHandler<T>();

            Bus.SerialisationRegister.AddSerialiser<T>(_serialisationFactory.GetSerialiser<T>());
            foreach (var region in Bus.Config.Regions)
            {
                Bus.AddMessageHandler(region, _subscriptionConfig.QueueName, () => handler);
            }
            var messageTypeName = GetMessageTypeName<T>();
            _log.LogInformation($"Added a message handler - MessageName: {messageTypeName}, QueueName: {_subscriptionConfig.QueueName}, HandlerName: {handler.GetType().Name}");

            return thing;
        }

        public IHaveFulfilledSubscriptionRequirements WithMessageHandler<T>(IHandlerResolver handlerResolver) where T : Message
        {
            var thing = _subscriptionConfig.SubscriptionType == SubscriptionType.PointToPoint
                ? PointToPointHandler<T>()
                : TopicHandler<T>();

            Bus.SerialisationRegister.AddSerialiser<T>(_serialisationFactory.GetSerialiser<T>());

            var resolutionContext = new HandlerResolutionContext(_subscriptionConfig.QueueName);
            var proposedHandler = handlerResolver.ResolveHandler<T>(resolutionContext);

            if (proposedHandler == null)
            {
                throw new HandlerNotRegisteredWithContainerException($"There is no handler for '{typeof(T).Name}' messages.");
            }

            foreach (var region in Bus.Config.Regions)
            {
                Bus.AddMessageHandler(region, _subscriptionConfig.QueueName, () => handlerResolver.ResolveHandler<T>(resolutionContext));
            }

            _log.LogInformation($"Added a message handler - Topic: {_subscriptionConfig.Topic}, QueueName: {_subscriptionConfig.QueueName}, HandlerName: IHandler<{typeof(T)}>");

            return thing;
        }

        private IHaveFulfilledSubscriptionRequirements TopicHandler<T>() where T : Message
        {
            var messageTypeName = GetMessageTypeName<T>();
            ConfigureSqsSubscriptionViaTopic(messageTypeName);

            foreach (var region in Bus.Config.Regions)
            {
                var queue = _amazonQueueCreator.EnsureTopicExistsWithQueueSubscribedAsync(region, Bus.SerialisationRegister, _subscriptionConfig).GetAwaiter().GetResult();
                CreateSubscriptionListener<T>(region, queue);
                _log.LogInformation($"Created SQS topic subscription - Topic: {_subscriptionConfig.Topic}, QueueName: {_subscriptionConfig.QueueName}");
            }

            return this;
        }

        private IHaveFulfilledSubscriptionRequirements PointToPointHandler<T>() where T : Message
        {
            var messageTypeName = GetMessageTypeName<T>();
            ConfigureSqsSubscription(messageTypeName);

            foreach (var region in Bus.Config.Regions)
            {
                var queue = _amazonQueueCreator.EnsureQueueExistsAsync(region, _subscriptionConfig).GetAwaiter().GetResult();
                CreateSubscriptionListener<T>(region, queue);
                _log.LogInformation($"Created SQS subscriber - MessageName: {messageTypeName}, QueueName: {_subscriptionConfig.QueueName}");
            }

            return this;
        }

        private void CreateSubscriptionListener<T>(string region, SqsQueueBase queue) where T : Message
        {
            var sqsSubscriptionListener = new SqsNotificationListener(queue, Bus.SerialisationRegister, Bus.Monitor, _loggerFactory, _subscriptionConfig.OnError, Bus.MessageLock, _subscriptionConfig.MessageBackoffStrategy);
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

        private void ConfigureSqsSubscriptionViaTopic(string messageTypeName)
        {
            var namingStrategy = GetNamingStrategy();
            _subscriptionConfig.PublishEndpoint = namingStrategy.GetTopicName(_subscriptionConfig.BaseTopicName, messageTypeName);
            _subscriptionConfig.Topic = namingStrategy.GetTopicName(_subscriptionConfig.BaseTopicName, messageTypeName);
            _subscriptionConfig.QueueName = namingStrategy.GetQueueName(_subscriptionConfig, messageTypeName);
            _subscriptionConfig.Validate();
        }

        private void ConfigureSqsSubscription(string messageTypeName)
        {
            _subscriptionConfig.ValidateSqsConfiguration();
            _subscriptionConfig.QueueName = GetNamingStrategy().GetQueueName(_subscriptionConfig, messageTypeName);
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
            var iterrogationBus = Bus as IAmJustInterrogating;
            return iterrogationBus.WhatDoIHave();
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
