using System;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Messages;

namespace JustSaying.Messaging
{
    public interface INotificationSubscriber
    {
        void AddMessageHandler<T>(IHandler<T> handler) where T : Message;
        void Listen();
        void StopListening();
    }
}