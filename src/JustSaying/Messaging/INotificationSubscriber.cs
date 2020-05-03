using System;
using System.Threading;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging
{
    public interface INotificationSubscriber : INotificationSubscriberInterrogation
    {
        bool IsListening { get; }
        void AddMessageHandler<T>(Func<IHandlerAsync<T>> handler, Func<T, string> uniqueKeySelector = default) where T : class;
        void Listen(CancellationToken cancellationToken);
        string Queue { get; }
    }
}
