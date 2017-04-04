using System.Collections.Generic;

namespace JustSaying.Messaging.Interrogation
{
    public interface IInterrogationResponse
    {
        IEnumerable<string> Regions { get; set; } 
        IEnumerable<ISubscriber> Subscribers { get; set; }
        IEnumerable<IPublisher> Publishers { get; set; }
    }
}