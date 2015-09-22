using System.Collections.Generic;

namespace JustSaying.Messaging.Interrogation
{
    public class InterrogationResponse : IInterrogationResponse
    {
        public InterrogationResponse(IEnumerable<ISubscriber> subscribers, IEnumerable<IPublisher> publishers)
        {
            Subscribers = subscribers;
            Publishers = publishers;
        }

        public IEnumerable<ISubscriber> Subscribers { get; set; }
        public IEnumerable<IPublisher> Publishers { get; set; }
    }
}