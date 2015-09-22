using System.Collections.Generic;

namespace JustSaying.Messaging.Interrogation
{
    public interface IInterrogationResponse
    {
        IEnumerable<ISubscriber> Subscribers { get;}
        IEnumerable<IPublisher> Publishers { get;}
    }
}