using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Messages;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;

namespace JustSaying
{
    public interface IAmJustSaying : IMessagePublisher
    {
        bool Listening { get; }
        void AddNotificationTopicSubscriber(string topic, INotificationSubscriber subscriber);
        void AddMessageHandler<T>(string topic, IHandler<T> handler) where T : Message;
        void AddMessagePublisher<T>(string topic, IMessagePublisher messagePublisher) where T : Message;
        void Start();
        void Stop();
        IMessagingConfig Config { get; }
        IMessageMonitor Monitor { get; set; }
        IMessageSerialisationRegister SerialisationRegister { get; }
    }
}
