using System;
using Amazon;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging;
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
    public class FluentNotificationStack : IMessagePublisher
    {
        private static NotificationStack _instance;
        private static readonly IMessageSerialisationRegister SerialisationRegister = new ReflectedMessageSerialisationRegister();

        public bool Listening { get { return (_instance != null) && _instance.Listening; } }

        private FluentNotificationStack(NotificationStack notificationStack)
        {
            _instance = notificationStack;
        }

        /// <summary>
        /// Create a new notification stack registration.
        /// </summary>
        /// <param name="component">Listening component</param>
        /// <returns></returns>
        public static FluentNotificationStack RegisterListeners(Component component)
        {
            return new FluentNotificationStack(new NotificationStack(component));
        }

        /// <summary>
        /// Subscribe to a topic using SQS.
        /// </summary>
        /// <param name="topic">Topic to listen in on</param>
        /// <returns></returns>
        public FluentNotificationStack WithSqsTopicSubscriber(NotificationTopic topic)
        {
            var endpoint = new Messaging.Lookups.SqsSubscribtionEndpointProvider().GetLocationEndpoint(_instance.Component, topic);
            var queue = new SqsQueueByUrl(endpoint, AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1));
            var sqsSub = new SqsNotificationListener(queue, SerialisationRegister);
            _instance.AddNotificationTopicSubscriber(topic, sqsSub);
            return this;
        }

        /// <summary>
        /// Set message handlers for the given topic
        /// </summary>
        /// <typeparam name="T">Message type to be handled</typeparam>
        /// <param name="topic">Topic message is published under</param>
        /// <param name="handler">Handler for the message type</param>
        /// <returns></returns>
        public FluentNotificationStack WithMessageHandler<T>(NotificationTopic topic, IHandler<T> handler) where T : Message
        {
            _instance.AddMessageHandler(topic, handler);
            return this;
        }

        //public FluentNotificationStack WithSnsMessagePublisher

        /// <summary>
        /// I'm done setting up. Fire this baby up...
        /// </summary>
        public void StartListening()
        {
            _instance.Start();
        }

        /// <summary>
        /// Gor graceful shutdown of all listening threads
        /// </summary>
        public void StopListening()
        {
            _instance.Stop();
        }

        public void Publish(Message message)
        {
            if (_instance == null)
                throw new InvalidOperationException("You must register for message publication before publishing a message");

            _instance.Publish(message);
        }
    }
}