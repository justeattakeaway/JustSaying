using System.Collections.Generic;
using Amazon;
using JustEat.AwsTools;
using SimplesNotificationStack.AwsTools;
using SimplesNotificationStack.Messaging;
using SimplesNotificationStack.Messaging.MessageHandling;
using SimplesNotificationStack.Messaging.MessageSerialisation;
using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Stack
{
    public class NotificationStack
    {
        internal Component Component { get; private set; }

        private readonly Dictionary<NotificationTopic, INotificationSubscriber> _notificationSubscribers;

        public NotificationStack(Component component)
        {
            Component = component;
            _notificationSubscribers = new Dictionary<NotificationTopic, INotificationSubscriber>();
        }

        public void AddNotificationTopicSubscriber(NotificationTopic topic, INotificationSubscriber subscriber)
        {
            _notificationSubscribers.Add(topic, subscriber);
        }

        public void AddMessageHandler(NotificationTopic topic, IHandler<Message> handler)
        {
            _notificationSubscribers[topic].AddMessageHandler(handler);
        }

        public void Start()
        {
            foreach (var subscription in _notificationSubscribers)
            {
                subscription.Value.Listen();
            }
        }
    }

    /// <summary>
    /// This is not the perfect shining example of a fluent API YET!
    /// Intended usage:
    /// 1. Call Register()
    /// 2. Set subscribers - WithSqsTopicSubscriber() / WithSnsTopicSubscriber() etc
    /// 3. Set Handlers - WithTopicMessageHandler()
    /// </summary>
    public class FluentNotificationStack
    {
        private static NotificationStack _instance;
        private static IMessageSerialisationRegister _serialisationRegister = new ReflectedMessageSerialisationRegister();

        private FluentNotificationStack(NotificationStack notificationStack)
        {
            _instance = notificationStack;
        }

        /// <summary>
        /// Create a new notification stack registration.
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public static FluentNotificationStack Register(Component component)
        {
            return new FluentNotificationStack(new NotificationStack(component));
        }

        /// <summary>
        /// Subscribe to a topic using SQS.
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public FluentNotificationStack WithSqsTopicSubscriber(NotificationTopic topic)
        {
            var endpoint = new Messaging.Lookups.SqsSubscribtionEndpointProvider().GetLocationEndpoint(_instance.Component, topic);
            var queue = new SqsQueueByUrl(endpoint, AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1));
            var sqsSub = new SqsNotificationListener(queue, _serialisationRegister);
            _instance.AddNotificationTopicSubscriber(topic, sqsSub);
            return this;
        }

        /// <summary>
        /// Set message handlers for the given topic
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public FluentNotificationStack WithTopicMessageHandler(NotificationTopic topic, IHandler<Message> handler)
        {
            _instance.AddMessageHandler(topic, handler);
            return this;
        }

        /// <summary>
        /// I'm done setting up. Fire this baby up...
        /// </summary>
        public void StartListening()
        {
            _instance.Start();
        }
    }
}
