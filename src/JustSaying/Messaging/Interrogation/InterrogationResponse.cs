using System.Collections.Generic;

namespace JustSaying.Messaging.Interrogation
{
    public class InterrogationResponse : IInterrogationResponse
    {
        public InterrogationResponse(string activeRegion, IEnumerable<ISubscriber> subscribers, IEnumerable<IPublisher> publishers)
        {
            Region = activeRegion;
            Subscribers = subscribers;
            Publishers = publishers;
        }

        public string Region { get; set; }
        public IEnumerable<ISubscriber> Subscribers { get; set; }
        public IEnumerable<IPublisher> Publishers { get; set; }
    }
}
