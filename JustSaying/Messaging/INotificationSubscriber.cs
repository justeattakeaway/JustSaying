using System;
using System.Threading;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging
{
    public interface INotificationSubscriber : INotificationSubscriberInterrogation
    {
        void AddMessageHandler<T>(Func<IHandlerAsync<T>> handler) where T : Message;
        void Listen(CancellationToken cancellationToken);
        string Queue { get; }
    }
}
