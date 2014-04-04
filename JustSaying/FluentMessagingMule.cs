using System;
using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Messages;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using NLog;
using JustSaying.Lookups;

namespace JustSaying
{
    /// <summary>
    /// Factory providing a messaging bus
    /// </summary>
    public static class Factory
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying"); // ToDo: Dangerous!

        public static IFluentMonitoring JustSaying(Action<INotificationStackConfiguration> configuration)
        {
            var config = new MessagingConfig();
            configuration.Invoke(config);
            config.Validate();

            if (string.IsNullOrWhiteSpace(config.Region))
            {
                config.Region = RegionEndpoint.EUWest1.SystemName;
                Log.Info("No Region was specified, using {0} by default.", config.Region);
            }

            return new JustSayingFluently(new JustSayingBus(config, new MessageSerialisationRegister()), new AmazonQueueCreator());
        }
    }


    /// <summary>
    /// Fluently configure a JustSaying message bus.
    /// Intended usage:
    /// 1. Factory.JustSaying(); // Gimme a bus
    /// 2. WithMonitoring(instance) // Ensure you monitor the messaging status
    /// 3. Set subscribers - WithSqsTopicSubscriber() / WithSnsTopicSubscriber() etc // ToDo: Shouldn't be enforced in base! Is a JE concern.
    /// 3. Set Handlers - WithTopicMessageHandler()
    /// </summary>
    public class JustSayingFluently : IFluentMonitoring, IFluentSubscription
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying"); // ToDo: Dangerous!
        private readonly IVerifyAmazonQueues _amazonQueueCreator;
        protected readonly IAmJustSaying Stack;
        private string _currnetTopic;

        public static string DefaultEndpoint
        {
            get { return RegionEndpoint.EUWest1.SystemName; }
        }

        internal protected JustSayingFluently(IAmJustSaying stack, IVerifyAmazonQueues queueCreator)
        {
            Stack = stack;
            _amazonQueueCreator = queueCreator;
        }

        /// <summary>
        /// Subscribe to a topic using SQS.
        /// </summary>
        /// <param name="topic">Topic to listen in on</param>
        /// <param name="messageRetentionSeconds">Time messages should be kept in this queue</param>
        /// <param name="visibilityTimeoutSeconds">Seconds message should be invisible to other other receiving components</param>
        /// <param name="instancePosition">Optional instance position as tagged by paas tools in AWS. Using this will cause the message to get handled by EACH instance in your cluster</param>
        /// <param name="onError">Optional error handler. Use this param to inject custom error handling from within the consuming application</param>
        /// <param name="maxAllowedMessagesInFlight">Configures the stack to use the Throttled handling strategy, configured to this level of concurrent messages in flight</param>
        /// <param name="messageProcessingStrategy">Hook to supply your own IMessageProcessingStrategy</param>
        /// <returns></returns>
        public IFluentSubscription WithSqsTopicSubscriber(string topic, int messageRetentionSeconds, int visibilityTimeoutSeconds = 30, int? instancePosition = null, Action<Exception> onError = null, int? maxAllowedMessagesInFlight = null, IMessageProcessingStrategy messageProcessingStrategy = null)
        {
            return WithSqsTopicSubscriber(cf =>
            {
                cf.Topic = topic;
                cf.MessageRetentionSeconds = messageRetentionSeconds;
                cf.VisibilityTimeoutSeconds = visibilityTimeoutSeconds;
                cf.InstancePosition = instancePosition;
                cf.OnError = onError;
                cf.MaxAllowedMessagesInFlight = maxAllowedMessagesInFlight;
                cf.MessageProcessingStrategy = messageProcessingStrategy;
            });
        }

        public virtual IPublishSubscribtionEndpointProvider CreateSubscriptiuonEndpointProvider(SqsConfiguration subscriptionConfig)
        {
            return new SqsSubscribtionEndpointProvider(subscriptionConfig);
        }
        public virtual IPublishEndpointProvider CreatePublisherEndpointProvider(SqsConfiguration subscriptionConfig)
        {
            return new SnsPublishEndpointProvider(subscriptionConfig.Topic);
        }
        public IFluentSubscription WithSqsTopicSubscriber(Action<SqsConfiguration> confBuilder)
        {
            var subscriptionConfig = new SqsConfiguration();
            confBuilder(subscriptionConfig);

            var subscriptionEndpointProvider = CreateSubscriptiuonEndpointProvider(subscriptionConfig);
            var publishEndpointProvider = CreatePublisherEndpointProvider(subscriptionConfig);

            subscriptionConfig.QueueName = subscriptionEndpointProvider.GetLocationName();
            subscriptionConfig.PublishEndpoint = publishEndpointProvider.GetLocationName();
            subscriptionConfig.Validate();

            var queue = _amazonQueueCreator.VerifyOrCreateQueue(Stack.Config.Region, Stack.SerialisationRegister, subscriptionConfig);

            var sqsSubscriptionListener = new SqsNotificationListener(queue, Stack.SerialisationRegister, new NullMessageFootprintStore(), Stack.Monitor, subscriptionConfig.OnError);
            Stack.AddNotificationTopicSubscriber(subscriptionConfig.Topic, sqsSubscriptionListener);
            
            if (subscriptionConfig.MaxAllowedMessagesInFlight.HasValue)
                sqsSubscriptionListener.WithMaximumConcurrentLimitOnMessagesInFlightOf(subscriptionConfig.MaxAllowedMessagesInFlight.Value);

            if (subscriptionConfig.MessageProcessingStrategy != null)
                sqsSubscriptionListener.WithMessageProcessingStrategy(subscriptionConfig.MessageProcessingStrategy);

            Log.Info(string.Format("Created SQS topic subscription - Topic: {0}, QueueName: {1}", subscriptionConfig.Topic, subscriptionConfig.QueueName));
            _currnetTopic = subscriptionConfig.Topic;

            return this;
        }

        public IFluentSubscription WithSqsTopicSubscriber(string topic, int messageRetentionSeconds, IMessageProcessingStrategy messageProcessingStrategy)
        {
            return WithSqsTopicSubscriber(topic, messageRetentionSeconds, 30, null, null, null,
                messageProcessingStrategy);
        }

        /// <summary>
        /// Register for publishing messages to SNS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <returns></returns>
        public IAmJustSayingFluently WithSnsMessagePublisher<T>(string topic) where T : Message
        {
            Log.Info("Added publisher");

            var endpointProvider = new SnsPublishEndpointProvider(topic);
            var eventPublisher = new SnsTopicByName(
                endpointProvider.GetLocationName(),
                AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(Stack.Config.Region)),
                Stack.SerialisationRegister);

            if (!eventPublisher.Exists())
                eventPublisher.Create();

            Stack.SerialisationRegister.AddSerialiser<T>(new ServiceStackSerialiser<T>());
            Stack.AddMessagePublisher<T>(topic, eventPublisher);

            Log.Info(string.Format("Created SNS topic publisher - Topic: {0}", topic));

            return this;
        }

        /// <summary>
        /// I'm done setting up. Fire up listening on this baby...
        /// </summary>
        public void StartListening()
        {
            Stack.Start();
            Log.Info("Started listening for messages");
        }

        /// <summary>
        /// Gor graceful shutdown of all listening threads
        /// </summary>
        public void StopListening()
        {
            Stack.Stop();
            Log.Info("Stopped listening for messages");
        }

        /// <summary>
        /// Publish a message to the stack.
        /// </summary>
        /// <param name="message"></param>
        public virtual void Publish(Message message)
        {
            if (Stack == null)
                throw new InvalidOperationException("You must register for message publication before publishing a message");
            
            Stack.Publish(message);
        }

        /// <summary>
        /// States whether the stack is listening for messages (subscriptions are running)
        /// </summary>
        public bool Listening { get { return (Stack != null) && Stack.Listening; } }
        
        #region Implementation of IFluentSubscription

        /// <summary>
        /// Set message handlers for the given topic
        /// </summary>
        /// <typeparam name="T">Message type to be handled</typeparam>
        /// <param name="handler">Handler for the message type</param>
        /// <returns></returns>
        public IFluentSubscription WithMessageHandler<T>(IHandler<T> handler) where T : Message
        {
            Stack.SerialisationRegister.AddSerialiser<T>(new ServiceStackSerialiser<T>());
            Stack.AddMessageHandler(_currnetTopic, handler);

            Log.Info(string.Format("Added a message handler - Topic: {0}, MessageType: {1}, HandlerName: {2}", _currnetTopic, typeof(T).Name, handler.GetType().Name));

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
            Stack.Monitor = messageMonitor;
            return this;
        }

        #endregion
    }

    public interface IAmJustSayingFluently : IMessagePublisher
    {
        IAmJustSayingFluently WithSnsMessagePublisher<T>(string topic) where T : Message;

        IFluentSubscription WithSqsTopicSubscriber(string topic, int messageRetentionSeconds,
            int visibilityTimeoutSeconds = 30, int? instancePosition = null, Action<Exception> onError = null,
            int? maxAllowedMessagesInFlight = null, IMessageProcessingStrategy messageProcessingStrategy = null);
        IFluentSubscription WithSqsTopicSubscriber(string topic, int messageRetentionSeconds, IMessageProcessingStrategy messageProcessingStrategy);
        IFluentSubscription WithSqsTopicSubscriber(Action<SqsConfiguration> confBuilder);

        void StartListening();
        void StopListening();
        bool Listening { get; }
    }

    public interface IFluentMonitoring
    {
        IAmJustSayingFluently WithMonitoring(IMessageMonitor messageMonitor);
    }

    public interface IFluentSubscription : IAmJustSayingFluently
    {
        IFluentSubscription WithMessageHandler<T>(IHandler<T> handler) where T : Message;
    }
}