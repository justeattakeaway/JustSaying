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
        void AddNotificationSubscriber(string region, INotificationSubscriber subscriber);
        void AddMessageHandler<T>(string region, string queueName, IHandlerAsync<T> futureHandler) where T : Message;

        // TODO - swap params
        void AddMessagePublisher<T>(IMessagePublisher messagePublisher, string region) where T : Message;
        void Start();
        void Stop();
        IMessagingConfig Config { get; }
        IMessageMonitor Monitor { get; set; }
        IMessageSerialisationRegister SerialisationRegister { get; }
        IMessageLock MessageLock { get; set; }
    }
}
