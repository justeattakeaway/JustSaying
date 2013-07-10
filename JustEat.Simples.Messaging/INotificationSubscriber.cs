using System;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Messaging
{
    public interface INotificationSubscriber
    {
        void AddMessageHandler<T>(IHandler<T> handler) where T : Message;
        void Listen();
        void StopListening();
    }
}