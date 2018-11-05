using System;
using System.Threading;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying
{
    public interface IAmJustSaying : IMessagePublisher
    {
        bool Listening { get; }
        void AddNotificationSubscriber(string region, INotificationSubscriber subscriber);
        void AddMessageHandler<T>(string region, string queueName, Func<IHandlerAsync<T>> handler) where T : Message;

        // TODO - swap params
        void AddMessagePublisher<T>(IMessagePublisher messagePublisher, string region) where T : Message;
        void Start(CancellationToken cancellationToken = default);
        IMessagingConfig Config { get; }
        IMessageMonitor Monitor { get; set; }
        IMessageSerialisationRegister SerialisationRegister { get; }
        IMessageLockAsync MessageLock { get; set; }
    }
}
