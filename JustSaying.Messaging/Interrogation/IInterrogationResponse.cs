using System.Collections.Generic;

namespace JustSaying.Messaging.Interrogation
{
    public interface IInterrogationResponse
    {
        ICollection<ISubscriber> Subscribers { get;}
        ICollection<IPublisher> Publishers { get;}
    }
}