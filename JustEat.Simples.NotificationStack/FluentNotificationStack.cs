using System;
using Amazon;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Lookups;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages;

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
        private readonly IMessageSerialisationRegister _serialisationRegister;

        public FluentNotificationStack(INotificationStack stack, IMessageSerialisationRegister serialisationRegister) : base(stack)
        {
            _serialisationRegister = serialisationRegister;
        }

        public static FluentNotificationStack Register(Component component, Action<INotificationStackConfiguration> action)
        {
            var config = new MessagingConfig();
            action.Invoke(config);

            if (string.IsNullOrWhiteSpace(config.Environment))
                throw new InvalidOperationException("Cannot have a blank entry for config.Environment");

            if (string.IsNullOrWhiteSpace(config.Tenant))
                throw new InvalidOperationException("Cannot have a blank entry for config.Tenant");

            return new FluentNotificationStack(new NotificationStack(component, config), new ReflectedMessageSerialisationRegister());
        }

        /// <summary>
        /// Create a new notification stack registration.
        /// </summary>
        /// <param name="component">Listening component</param>
        /// <param name="config">Configuration items</param>
        /// <returns></returns>
        [Obsolete("Use Register(Component component, Action<INotificationStackConfiguration> action) instead,", false)]
        public static FluentNotificationStack Register(Component component, IMessagingConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Environment))
                throw new InvalidOperationException("Cannot have a blank entry for config.Environment");

            if (string.IsNullOrWhiteSpace(config.Tenant))
                throw new InvalidOperationException("Cannot have a blank entry for config.Tenant");

            return new FluentNotificationStack(new NotificationStack(component, config), new ReflectedMessageSerialisationRegister());
        }

        /// <summary>
        /// Subscribe to a topic using SQS.
        /// </summary>
        /// <param name="topic">Topic to listen in on</param>
        /// <param name="messageRetentionSeconds">Time messages should be kept in this queue</param>
        /// <returns></returns>
        public FluentSubscription WithSqsTopicSubscriber(NotificationTopic topic, int messageRetentionSeconds)
        {
            var endpointProvider = new SqsSubscribtionEndpointProvider(Stack.Config);
            var queue = new SqsQueueByName(endpointProvider.GetLocationName(Stack.Component, topic), AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1));
            var eventTopic = new SnsTopicByName(new SnsPublishEndpointProvider(Stack.Config).GetLocationName(topic), AWSClientFactory.CreateAmazonSNSClient(RegionEndpoint.EUWest1), _serialisationRegister);

            if (!queue.Exists())
                queue.Create(messageRetentionSeconds);

            if (!eventTopic.Exists())
                eventTopic.Create();

            if (!eventTopic.IsSubscribed(queue))
                eventTopic.Subscribe(queue);

            var sqsSubscriptionListener = new SqsNotificationListener(queue, _serialisationRegister);
            Stack.AddNotificationTopicSubscriber(topic, sqsSubscriptionListener);
            return new FluentSubscription(Stack, _serialisationRegister, topic);
        }

        /// <summary>
        /// Register for publishing messages to SNS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <returns></returns>
        public FluentNotificationStack WithSnsMessagePublisher<T>(NotificationTopic topic) where T : Message
        {
            var endpointProvider = new SnsPublishEndpointProvider(Stack.Config);
            var eventPublisher = new SnsTopicByName(endpointProvider.GetLocationName(topic), AWSClientFactory.CreateAmazonSNSClient(RegionEndpoint.EUWest1), _serialisationRegister);

            if (!eventPublisher.Exists())
                eventPublisher.Create();

            Stack.AddMessagePublisher<T>(topic, eventPublisher);

            return this;
        }

        /// <summary>
        /// I'm done setting up. Fire up listening on this baby...
        /// </summary>
        public void StartListening()
        {
            Stack.Start();
        }

        /// <summary>
        /// Gor graceful shutdown of all listening threads
        /// </summary>
        public void StopListening()
        {
            Stack.Stop();
        }

        /// <summary>
        /// Publish a message to the stack.
        /// </summary>
        /// <param name="message"></param>
        public void Publish(Message message)
        {
            if (Stack == null)
                throw new InvalidOperationException("You must register for message publication before publishing a message");

            message.RaisingComponent = Stack.Component;
            Stack.Publish(message);
        }

        /// <summary>
        /// States whether the stack is listening for messages (subscriptions are running)
        /// </summary>
        public bool Listening { get { return (Stack != null) && Stack.Listening; } }
    }

    public class FluentSubscription : FluentNotificationStack
    {
        private readonly NotificationTopic _topic;

        public FluentSubscription(INotificationStack stack, IMessageSerialisationRegister serialisationRegister, NotificationTopic topic)
            : base(stack, serialisationRegister)
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
            Stack.AddMessageHandler(_topic, handler);
            return this;
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
        public string Tenant { get; set; }
        public string Environment { get; set; }
        public int PublishFailureReAttempts { get; set; }
        public int PublishFailureBackoffMilliseconds { get; set; }
    }

    public interface INotificationStackConfiguration
    {
        string Tenant { get; set; }
        string Environment { get; set; }
        int PublishFailureReAttempts { get; set; }
        int PublishFailureBackoffMilliseconds { get; set; }
    }

}