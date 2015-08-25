using System.Collections.Generic;

namespace JustSaying.Messaging.Interrogation
{
    public class InterrogationResponse : IInterrogationResponse
    {
        public InterrogationResponse()
        {
            Subscribers = new List<ISubscriber>();
            Publishers = new List<IPublisher>();
        }

        public ICollection<ISubscriber> Subscribers { get; }
        public ICollection<IPublisher> Publishers { get; }
    }
}