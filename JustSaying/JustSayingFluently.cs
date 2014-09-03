using System;
using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using NLog;
using JustSaying.Lookups;

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
    public class JustSayingFluently : ISubscriberIntoQueue, IHaveFulfilledSubscriptionRequirements, IHaveFulfilledPublishRequirements, IMayWantOptionalSettings
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying"); // ToDo: Dangerous!
        private readonly IVerifyAmazonQueues _amazonQueueCreator;
        protected readonly IAmJustSaying Bus;
        private SqsReadConfiguration _subscriptionConfig = new SqsReadConfiguration();
        private IMessageSerialisationFactory _serialisationFactory;

        internal protected JustSayingFluently(IAmJustSaying bus, IVerifyAmazonQueues queueCreator)
        {
            Bus = bus;
            _amazonQueueCreator = queueCreator;
        }

        // ToDo: Move these into the factory class?
        public virtual IPublishSubscribtionEndpointProvider CreateSubscriptiuonEndpointProvider(SqsReadConfiguration subscriptionConfig)
        {
            return new SqsSubscribtionEndpointProvider(subscriptionConfig);
        }
        public virtual IPublishEndpointProvider CreatePublisherEndpointProvider(SqsReadConfiguration subscriptionConfig)
        {
            return new SnsPublishEndpointProvider(subscriptionConfig.Topic);
        }

        /// <summary>
        /// Register for publishing messages to SNS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IHaveFulfilledPublishRequirements WithSnsMessagePublisher<T>() where T : Message
        {
            Log.Info("Adding SNS publisher");
            _subscriptionConfig.Topic = typeof(T).Name.ToLower();
            var publishEndpointProvider = CreatePublisherEndpointProvider(_subscriptionConfig);
            var eventPublisher = new SnsTopicByName(
                publishEndpointProvider.GetLocationName(),
                AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(Bus.Config.Region)),
                Bus.SerialisationRegister);

            if (!eventPublisher.Exists())
                eventPublisher.Create();

            Bus.SerialisationRegister.AddSerialiser<T>(_serialisationFactory.GetSerialiser<T>());

            Bus.AddMessagePublisher<T>(eventPublisher);

            Log.Info(string.Format("Created SNS topic publisher - Topic: {0}", _subscriptionConfig.Topic));

            return this;
        }


        /// <summary>
        /// Register for publishing messages to SQS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IHaveFulfilledPublishRequirements WithSqsMessagePublisher<T>(Action<SqsBasicConfiguration> configBuilder) where T : Message
        {
            Log.Info("Adding SQS publisher");

            var config = new SqsBasicConfiguration();
            configBuilder(config);

            _subscriptionConfig.Topic = typeof(T).Name.ToLower();
            var publishEndpointProvider = CreatePublisherEndpointProvider(_subscriptionConfig);
            var eventPublisher = new SqsPublisher(
                publishEndpointProvider.GetLocationName(),
                AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.GetBySystemName(Bus.Config.Region)),
                0, //ToDo: Think about this in context of publishing
                Bus.SerialisationRegister);

            if (!eventPublisher.Exists())
                eventPublisher.Create(config.MessageRetentionSeconds, 0, config.VisibilityTimeoutSeconds, config.ErrorQueueOptOut, config.RetryCountBeforeSendingToErrorQueue);

            Bus.SerialisationRegister.AddSerialiser<T>(_serialisationFactory.GetSerialiser<T>());

            Bus.AddMessagePublisher<T>(eventPublisher);

            Log.Info(string.Format("Created SQS publisher - MessageName: {0}", _subscriptionConfig.Topic));

            return this;
        }

        /// <summary>
        /// I'm done setting up. Fire up listening on this baby...
        /// </summary>
        public void StartListening()
        {
            Bus.Start();
            Log.Info("Started listening for messages");
        }

        /// <summary>
        /// Gor graceful shutdown of all listening threads
        /// </summary>
        public void StopListening()
        {
            Bus.Stop();
            Log.Info("Stopped listening for messages");
        }

        /// <summary>
        /// Publish a message to the stack.
        /// </summary>
        /// <param name="message"></param>
        public virtual void Publish(Message message)
        {
            if (Bus == null)
                throw new InvalidOperationException("You must register for message publication before publishing a message");
            
            Bus.Publish(message);
        }

        /// <summary>
        /// States whether the stack is listening for messages (subscriptions are running)
        /// </summary>
        public bool Listening { get { return (Bus != null) && Bus.Listening; } }

        public IMayWantOptionalSettings WithSerialisationFactory(IMessageSerialisationFactory factory)
        {
            _serialisationFactory = factory;
            return this;
        }

        public IMayWantOptionalSettings WithMessageLockStoreOf(IMessageLock messageLock)
        {
            Bus.MessageLock = messageLock;
            return this;
        }

        public IFluentSubscription ConfigureSubscriptionWith(Action<SqsReadConfiguration> configBuilder)
        {
            configBuilder(_subscriptionConfig);
            return this;
        }

        public ISubscriberIntoQueue WithSqsTopicSubscriber()
        {
            _subscriptionConfig = new SqsReadConfiguration();
            return this;
        }

        #region Implementation of Queue Subscription

        private bool _subscriptionConfigured;

        public IFluentSubscription IntoQueue(string queuename)
        {
            _subscriptionConfigured = false;
            _subscriptionConfig.QueueName = queuename;
            return this;
        }

        /// <summary>
        /// Set message handlers for the given topic
        /// </summary>
        /// <typeparam name="T">Message type to be handled</typeparam>
        /// <param name="handler">Handler for the message type</param>
        /// <returns></returns>
        public IHaveFulfilledSubscriptionRequirements WithMessageHandler<T>(IHandler<T> handler) where T : Message
        {
            _subscriptionConfig.Topic = typeof (T).Name.ToLower();
            var publishEndpointProvider = CreatePublisherEndpointProvider(_subscriptionConfig);

            _subscriptionConfig.PublishEndpoint = publishEndpointProvider.GetLocationName();
            _subscriptionConfig.Validate();

            var subscriptionEndpointProvider = CreateSubscriptiuonEndpointProvider(_subscriptionConfig);
            _subscriptionConfig.QueueName = subscriptionEndpointProvider.GetLocationName();
            var queue = _amazonQueueCreator.VerifyOrCreateQueue(Bus.Config.Region, Bus.SerialisationRegister, _subscriptionConfig);

            var sqsSubscriptionListener = new SqsNotificationListener(queue, Bus.SerialisationRegister, Bus.Monitor, _subscriptionConfig.OnError, Bus.MessageLock);
            Bus.AddNotificationTopicSubscriber(_subscriptionConfig.Topic, sqsSubscriptionListener);

            if (_subscriptionConfig.MaxAllowedMessagesInFlight.HasValue)
                sqsSubscriptionListener.WithMaximumConcurrentLimitOnMessagesInFlightOf(_subscriptionConfig.MaxAllowedMessagesInFlight.Value);

            if (_subscriptionConfig.MessageProcessingStrategy != null)
                sqsSubscriptionListener.WithMessageProcessingStrategy(_subscriptionConfig.MessageProcessingStrategy);

            Log.Info(string.Format("Created SQS topic subscription - Topic: {0}, QueueName: {1}", _subscriptionConfig.Topic, _subscriptionConfig.QueueName));

            _subscriptionConfigured = true;

            if (!_subscriptionConfigured)
                ConfigureSubscriptionWith(conf => conf.ErrorQueueOptOut = false);

            Bus.SerialisationRegister.AddSerialiser<T>(_serialisationFactory.GetSerialiser<T>());
            Bus.AddMessageHandler(handler);

            Log.Info(string.Format("Added a message handler - Topic: {0}, MessageType: {1}, HandlerName: {2}", _subscriptionConfig.Topic, typeof(T).Name, handler.GetType().Name));

            return this;
        }

        #endregion

        #region Implementation of IFluentMonitoring

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

        #endregion

        public IHaveFulfilledPublishRequirements ConfigurePublisherWith(Action<IPublishConfiguration> confBuilder)
        {
            confBuilder(Bus.Config as IPublishConfiguration);
            Bus.Config.Validate();

            return this;
        }
    }

    public interface IMayWantOptionalSettings : IMayWantMonitoring, IMayWantMessageLockStore, IMayWantCustomSerialisation { }

    public interface IMayWantMonitoring : IAmJustSayingFluently
    {
        IMayWantOptionalSettings WithMonitoring(IMessageMonitor messageMonitor);
    }

    public interface IMayWantMessageLockStore : IAmJustSayingFluently
    {
        IMayWantOptionalSettings WithMessageLockStoreOf(IMessageLock messageLock);
    }

    public interface IMayWantCustomSerialisation : IAmJustSayingFluently
    {
        IMayWantOptionalSettings WithSerialisationFactory(IMessageSerialisationFactory factory);
    }

    public interface IAmJustSayingFluently : IMessagePublisher
    {
        IHaveFulfilledPublishRequirements ConfigurePublisherWith(Action<IPublishConfiguration> confBuilder);
        IHaveFulfilledPublishRequirements WithSnsMessagePublisher<T>() where T : Message;
        IHaveFulfilledPublishRequirements WithSqsMessagePublisher<T>(Action<SqsBasicConfiguration> config) where T : Message;
        ISubscriberIntoQueue WithSqsTopicSubscriber();
        void StartListening();
        void StopListening();
        bool Listening { get; }
    }

    public interface IFluentSubscription
    {
        IHaveFulfilledSubscriptionRequirements WithMessageHandler<T>(IHandler<T> handler) where T : Message;
        IFluentSubscription ConfigureSubscriptionWith(Action<SqsReadConfiguration> config);
    }

    public interface IHaveFulfilledSubscriptionRequirements : IAmJustSayingFluently, IFluentSubscription
    {
        
    }

    public interface ISubscriberIntoQueue
    {
        IFluentSubscription IntoQueue(string queuename);
    }

    public interface IHaveFulfilledPublishRequirements : IAmJustSayingFluently
    {
    }
}