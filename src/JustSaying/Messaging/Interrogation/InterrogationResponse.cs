using System.Collections.Generic;

namespace JustSaying.Messaging.Interrogation
{
    public class InterrogationResponse : IInterrogationResponse
    {
        public InterrogationResponse(IEnumerable<string> regions, IEnumerable<ISubscriber> subscribers, IEnumerable<IPublisher> publishers)
        {
            Regions = regions;
            Subscribers = subscribers;
            Publishers = publishers;
        }

        public IEnumerable<string> Regions { get; set; }
        public IEnumerable<ISubscriber> Subscribers { get; set; }
        public IEnumerable<IPublisher> Publishers { get; set; }
    }
}