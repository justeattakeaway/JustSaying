using System;
using Amazon;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Lookups;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.Simples.NotificationStack.Stack.Monitoring;
using JustEat.StatsD;
using NLog;

namespace JustEat.Simples.NotificationStack.Stack
{
    /// <summary>
    /// This is not the perfect shining example of a fluent API YET!
    /// Intended usage:
    /// 1. Call Register()
    /// 2. Set subscribers - WithSqsTopicSubscriber() / WithSnsTopicSubscriber() etc
    /// 3. Set Handlers - WithTopicMessageHandler()
    /// </summary>
    public class FluentNotificationStack : FluentStackBase, IMessagePublisher
    {
        private static readonly Logger Log = LogManager.GetLogger("JustEat.Simples.NotificationStack");

        public FluentNotificationStack(INotificationStack stack) : base(stack)
        {
        }

        public static FluentMonitoring Register(Action<INotificationStackConfiguration> configuration)
        {
            var config = new MessagingConfig();
            configuration.Invoke(config);

            if (string.IsNullOrWhiteSpace(config.Environment))
                throw new ArgumentNullException("config.Environment", "Cannot have a blank entry for config.Environment");

            if (string.IsNullOrWhiteSpace(config.Tenant))
                throw new ArgumentNullException("config.Tenant", "Cannot have a blank entry for config.Tenant");

            if (string.IsNullOrWhiteSpace(config.Component))
                throw new ArgumentNullException("config.Component", "Cannot have a blank entry for config.Component");

            return new FluentMonitoring(new NotificationStack(config, new MessageSerialisationRegister()));
        }

        /// <summary>
        /// Create a new notification stack registration.
        /// </summary>
        /// <param name="component">Listening component</param>
        /// <param name="config">Configuration items</param>
        /// <returns></returns>
        [Obsolete("Use Register(Component component, Action<INotificationStackConfiguration> action) instead,", false)]
        public static FluentMonitoring Register(IMessagingConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Environment))
                throw new InvalidOperationException("Cannot have a blank entry for config.Environment");

            if (string.IsNullOrWhiteSpace(config.Tenant))
                throw new InvalidOperationException("Cannot have a blank entry for config.Tenant");

            return new FluentMonitoring(new NotificationStack(config, new MessageSerialisationRegister()));
        }

        /// <summary>
        /// Subscribe to a topic using SQS.
        /// </summary>
        /// <param name="topic">Topic to listen in on</param>
        /// <param name="messageRetentionSeconds">Time messages should be kept in this queue</param>
        /// <param name="visibilityTimeoutSeconds">Seconds message should be invisible to other other receiving components</param>
        /// <param name="instancePosition">Optional instance position as tagged by paas tools in AWS. Using this will cause the message to get handled by EACH instance in your cluster</param>
        /// <returns></returns>
        public FluentSubscription WithSqsTopicSubscriber(string topic, int messageRetentionSeconds, int visibilityTimeoutSeconds = 30, int? instancePosition = null)
        {
            var endpointProvider = new SqsSubscribtionEndpointProvider(Stack.Config);
            var queueName = instancePosition.HasValue
                                ? endpointProvider.GetLocationName(Stack.Config.Component, topic, instancePosition.Value)
                                : endpointProvider.GetLocationName(Stack.Config.Component, topic);
            var queue = new SqsQueueByName(queueName, AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1));
            var eventTopic = new SnsTopicByName(new SnsPublishEndpointProvider(Stack.Config).GetLocationName(topic), AWSClientFactory.CreateAmazonSNSClient(RegionEndpoint.EUWest1), Stack.SerialisationRegister);

            if (!queue.Exists())
                queue.Create(messageRetentionSeconds, 0, visibilityTimeoutSeconds);

            if (!eventTopic.Exists())
                eventTopic.Create();

            if (!eventTopic.IsSubscribed(queue))
                eventTopic.Subscribe(queue);

            if (!queue.HasPermission(eventTopic))
                queue.AddPermission(eventTopic);

            var sqsSubscriptionListener = new SqsNotificationListener(queue, Stack.SerialisationRegister, new NullMessageFootprintStore(), Stack.Monitor);
            Stack.AddNotificationTopicSubscriber(topic, sqsSubscriptionListener);
            
            Log.Info(string.Format("Created SQS topic subscription - Component: {0}, Topic: {1}, QueueName: {2}", Stack.Config.Component, topic, queue.QueueNamePrefix));

            return new FluentSubscription(Stack, topic);
        }

        /// <summary>
        /// Register for publishing messages to SNS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <returns></returns>
        public FluentNotificationStack WithSnsMessagePublisher<T>(string topic) where T : Message
        {
            Log.Info("Added publisher");

            var endpointProvider = new SnsPublishEndpointProvider(Stack.Config);
            var eventPublisher = new SnsTopicByName(endpointProvider.GetLocationName(topic), AWSClientFactory.CreateAmazonSNSClient(RegionEndpoint.EUWest1), Stack.SerialisationRegister);

            if (!eventPublisher.Exists())
                eventPublisher.Create();

            Stack.SerialisationRegister.AddSerialiser<T>(new ServiceStackSerialiser<T>());
            Stack.AddMessagePublisher<T>(topic, eventPublisher);

            Log.Info(string.Format("Created SNS topic publisher - Component: {0}, Topic: {1}", Stack.Config.Component, topic));

            return this;
        }

        /// <summary>
        /// I'm done setting up. Fire up listening on this baby...
        /// </summary>
        public void StartListening()
        {
            Stack.Start();
            Log.Info("Started listening for messages: Component: " + Stack.Config.Component);
        }

        /// <summary>
        /// Gor graceful shutdown of all listening threads
        /// </summary>
        public void StopListening()
        {
            Stack.Stop();
            Log.Info("Stopped listening for messages: Component: " + Stack.Config.Component);
        }

        /// <summary>
        /// Publish a message to the stack.
        /// </summary>
        /// <param name="message"></param>
        public void Publish(Message message)
        {
            if (Stack == null)
                throw new InvalidOperationException("You must register for message publication before publishing a message");

            message.RaisingComponent = Stack.Config.Component;
            message.Tenant = Stack.Config.Tenant;

            Stack.Publish(message);
        }

        /// <summary>
        /// States whether the stack is listening for messages (subscriptions are running)
        /// </summary>
        public bool Listening { get { return (Stack != null) && Stack.Listening; } }
    }

    public class FluentSubscription : FluentNotificationStack
    {
        private readonly string _topic;
        private static readonly Logger Log = LogManager.GetLogger("JustEat.Simples.NotificationStack");

        public FluentSubscription(INotificationStack stack, string topic)
            : base(stack)
        {
            _topic = topic;
        }

        /// <summary>
        /// Set message handlers for the given topic
        /// </summary>
        /// <typeparam name="T">Message type to be handled</typeparam>
        /// <param name="topic">Topic message is published under</param>
        /// <param name="handler">Handler for the message type</param>
        /// <returns></returns>
        public FluentSubscription WithMessageHandler<T>(IHandler<T> handler) where T : Message
        {
            Stack.SerialisationRegister.AddSerialiser<T>(new ServiceStackSerialiser<T>());
            Stack.AddMessageHandler(_topic, handler);

            Log.Info(string.Format("Added a message handler - Component: {0}, Topic: {1}, MessageType: {2}, HandlerName: {3}", Stack.Config.Component, _topic, typeof(T).Name, handler.GetType().Name));

            return this;
        }
    }

    public class FluentMonitoring : FluentStackBase
    {
        public FluentMonitoring(INotificationStack stack) : base(stack)
        {
        }

        /// <summary>
        /// Provide your own monitoring implementation
        /// </summary>
        /// <param name="messageMonitor">Monitoring class to be used</param>
        /// <returns></returns>
        public FluentNotificationStack WithMonitoring(IMessageMonitor messageMonitor)
        {
            Stack.Monitor = messageMonitor;
            return new FluentNotificationStack(Stack);
        }

        /// <summary>
        /// Use the default JustEat StatsD Monitoring tooling
        /// </summary>
        /// <param name="publisher">The JustEat.StatsD publisher you use in your application (suggest immediate publisher)</param>
        /// <returns></returns>
        public FluentNotificationStack WithStatsDMonitoring(IStatsDPublisher publisher)
        {
            Stack.Monitor = new StatsDMessageMonitor(publisher);
            return new FluentNotificationStack(Stack);
        }
    }

    public abstract class FluentStackBase
    {
        protected readonly INotificationStack Stack;

        protected FluentStackBase(INotificationStack stack)
        {
            Stack = stack;
        }
    }

    public class MessagingConfig : IMessagingConfig, INotificationStackConfiguration
    {
        public string Component { get; set; }
        public string Tenant { get; set; }
        public string Environment { get; set; }
        public int PublishFailureReAttempts { get; set; }
        public int PublishFailureBackoffMilliseconds { get; set; }
    }

    public interface INotificationStackConfiguration
    {
        string Component { get; set; }
        string Tenant { get; set; }
        string Environment { get; set; }
        int PublishFailureReAttempts { get; set; }
        int PublishFailureBackoffMilliseconds { get; set; }
    }
}