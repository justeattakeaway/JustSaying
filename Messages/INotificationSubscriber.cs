using System;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Messaging
{
    public interface INotificationSubscriber
    {
        void AddMessageHandler<T>(Action<T> handler) where T : Message;
        void Listen();
    }
}