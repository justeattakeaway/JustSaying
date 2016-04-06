using System;
using System.Threading.Tasks;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging
{
    public interface INotificationSubscriber : INotificationSubscriberInterrogation
    {
        void AddMessageHandler<T>(Func<IHandler<T>> handler) where T : Message;
        void Listen();
        Task StopListening();
        string Queue { get; }
    }
}