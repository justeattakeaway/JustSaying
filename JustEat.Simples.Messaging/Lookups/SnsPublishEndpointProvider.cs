using System;

namespace JustEat.Simples.NotificationStack.Messaging.Lookups
{
    public class SnsPublishEndpointProvider : IPublishEndpointProvider
    {
        private readonly IMessagingConfig _config;

        public SnsPublishEndpointProvider(IMessagingConfig config)
        {
            _config = config;
        }

        public string GetLocationEndpoint(string location)
        {
            throw new NotImplementedException("Not implemented yet. Come back later");
            //// Eventually this should come from the Settings API (having been published somewhere by the create process).
            //switch (location)
            //{
            //    case NotificationTopic.OrderDispatch:
            //        return "arn:aws:sns:eu-west-1:507204202721:uk-qa12-order-dispatch";

            //    case NotificationTopic.CustomerCommunication:
            //        return "arn:aws:sns:eu-west-1:507204202721:uk-qa12-customer-order-communication";
            //} 

            //throw new IndexOutOfRangeException("There is no endpoint defined for the provided location type");
        }

        public string GetLocationName(string location)
        {
            return String.Join("-", new[] { _config.Tenant, _config.Environment, location }).ToLower();
        }
    }
}