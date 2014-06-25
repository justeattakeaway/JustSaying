using System;
using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Messages;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
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
    public class JustSayingFluently : ISubscriberIntoQueue, IHaveFulfilledSubscriptionRequirements, IHaveFulfilledPublishRequirements, IMayWantMonitoring
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying"); // ToDo: Dangerous!
        private readonly IVerifyAmazonQueues _amazonQueueCreator;
        protected readonly IAmJustSaying Bus;
        private SqsConfiguration _subscriptionConfig = new SqsConfiguration();

        internal protected JustSayingFluently(IAmJustSaying bus, IVerifyAmazonQueues queueCreator)
        {
            Bus = bus;
            _amazonQueueCreator = queueCreator;
        }

        // ToDo: Move these into the factory class?
        public virtual IPublishSubscribtionEndpointProvider CreateSubscriptiuonEndpointProvider(SqsConfiguration subscriptionConfig)
        {
            return new SqsSubscribtionEndpointProvider(subscriptionConfig);
        }
        public virtual IPublishEndpointProvider CreatePublisherEndpointProvider(SqsConfiguration subscriptionConfig)
        {
            return new SnsPublishEndpointProvider(subscriptionConfig.Topic);
        }
        
        /// <summary>
        /// Register for publishing messages to SNS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <returns></returns>
        public IHaveFulfilledPublishRequirements WithSnsMessagePublisher<T>(string topic) where T : Message
        {
            Log.Info("Added publisher");
            _subscriptionConfig.Topic = topic;
            var publishEndpointProvider = CreatePublisherEndpointProvider(_subscriptionConfig);
            var eventPublisher = new SnsTopicByName(
                publishEndpointProvider.GetLocationName(),
                AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(Bus.Config.Region)),
                Bus.SerialisationRegister);

            if (!eventPublisher.Exists())
                eventPublisher.Create();

            Bus.SerialisationRegister.AddSerialiser<T>(new ServiceStackSerialiser<T>());
            Bus.AddMessagePublisher<T>(topic, eventPublisher);

            Log.Info(string.Format("Created SNS topic publisher - Topic: {0}", topic));

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

        public IFluentSubscription ConfigureSubscriptionWith(Action<SqsConfiguration> configBuilder)
        {
            configBuilder(_subscriptionConfig);
            var publishEndpointProvider = CreatePublisherEndpointProvider(_subscriptionConfig);

            _subscriptionConfig.PublishEndpoint = publishEndpointProvider.GetLocationName();
            _subscriptionConfig.Validate();

            var subscriptionEndpointProvider = CreateSubscriptiuonEndpointProvider(_subscriptionConfig);
            _subscriptionConfig.QueueName = subscriptionEndpointProvider.GetLocationName();
            var queue = _amazonQueueCreator.VerifyOrCreateQueue(Bus.Config.Region, Bus.SerialisationRegister, _subscriptionConfig);

            var sqsSubscriptionListener = new SqsNotificationListener(queue, Bus.SerialisationRegister, Bus.Monitor, _subscriptionConfig.OnError);
            Bus.AddNotificationTopicSubscriber(_subscriptionConfig.Topic, sqsSubscriptionListener);

            if (_subscriptionConfig.MaxAllowedMessagesInFlight.HasValue)
                sqsSubscriptionListener.WithMaximumConcurrentLimitOnMessagesInFlightOf(_subscriptionConfig.MaxAllowedMessagesInFlight.Value);

            if (_subscriptionConfig.MessageProcessingStrategy != null)
                sqsSubscriptionListener.WithMessageProcessingStrategy(_subscriptionConfig.MessageProcessingStrategy);

            Log.Info(string.Format("Created SQS topic subscription - Topic: {0}, QueueName: {1}", _subscriptionConfig.Topic, _subscriptionConfig.QueueName));

            _subscriptionConfigured = true;
            return this;
        }

        public ISubscriberIntoQueue WithSqsTopicSubscriber(string topic)
        {
            _subscriptionConfig = new SqsConfiguration {Topic = topic};
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
            if (!_subscriptionConfigured)
                ConfigureSubscriptionWith(conf => conf.ErrorQueueOptOut = false);

            Bus.SerialisationRegister.AddSerialiser<T>(new ServiceStackSerialiser<T>());
            Bus.AddMessageHandler(_subscriptionConfig.Topic, handler);

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
        public IAmJustSayingFluently WithMonitoring(IMessageMonitor messageMonitor)
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

    public interface IMayWantMonitoring : IAmJustSayingFluently
    {
        IAmJustSayingFluently WithMonitoring(IMessageMonitor messageMonitor);
    }

    public interface IAmJustSayingFluently : IMessagePublisher
    {
        IHaveFulfilledPublishRequirements ConfigurePublisherWith(Action<IPublishConfiguration> confBuilder);
        ISubscriberIntoQueue WithSqsTopicSubscriber(string topic);
        void StartListening();
        void StopListening();
        bool Listening { get; }
    }

    public interface IFluentSubscription
    {
        IHaveFulfilledSubscriptionRequirements WithMessageHandler<T>(IHandler<T> handler) where T : Message;
        IFluentSubscription ConfigureSubscriptionWith(Action<SqsConfiguration> config);
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
        IHaveFulfilledPublishRequirements WithSnsMessagePublisher<T>(string topic) where T : Message;
    }


}