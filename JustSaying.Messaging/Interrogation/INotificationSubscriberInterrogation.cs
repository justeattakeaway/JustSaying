using System.Collections.Generic;

namespace JustSaying.Messaging.Interrogation
{
    public interface INotificationSubscriberInterrogation
    {
        ICollection<ISubscriber> Subscribers { get; set; }
    }
}