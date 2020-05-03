using System;
using System.Threading;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;

namespace JustSaying
{
    public interface IAmJustSaying : IMessagePublisher
    {
        void AddNotificationSubscriber(string region, INotificationSubscriber subscriber);

        void AddMessageHandler<T>(string region, string queueName, Func<IHandlerAsync<T>> handler) where T : class;

        // TODO - swap params
        void AddMessagePublisher<T>(IMessagePublisher messagePublisher, string region) where T : class;

        void Start(CancellationToken cancellationToken = default);

        IMessagingConfig Config { get; }

        IMessageMonitor Monitor { get; set; }

        IMessageSerializationRegister SerializationRegister { get; }

        IMessageLockAsync MessageLock { get; set; }

        IMessageContextAccessor MessageContextAccessor { get; set; }
    }
}
