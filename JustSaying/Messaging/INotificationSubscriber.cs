using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging
{
    public interface INotificationSubscriber : INotificationSubscriberInterrogation
    {
        void AddMessageHandler<T>(FutureHandler<T> futureHandler) where T : Message;
        void Listen();
        void StopListening();
        string Queue { get; }
    }
}