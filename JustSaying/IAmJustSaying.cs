using System;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;

namespace JustSaying
{
    public interface IAmJustSaying : IMessagePublisher
    {
        bool Listening { get; }
        void AddNotificationTopicSubscriber(string topic, INotificationSubscriber subscriber);
        void AddMessageHandler<T>(Func<IHandler<T>> handler) where T : Message;
        void AddMessagePublisher<T>(IMessagePublisher messagePublisher, string region) where T : Message;
        void Start();
        void Stop();
        IMessagingConfig Config { get; }
        IMessageMonitor Monitor { get; set; }
        IMessageSerialisationRegister SerialisationRegister { get; }
        IMessageLock MessageLock { get; set; }
    }
}
