using System;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging
{
    public interface INotificationSubscriber
    {
        void AddMessageHandler<T>(Func<IHandler<T>> handler) where T : Message;
        void Listen();
        void StopListening();
        string Queue { get; }
    }
}