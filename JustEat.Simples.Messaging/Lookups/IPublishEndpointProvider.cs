using System;

namespace JustEat.Simples.NotificationStack.Messaging.Lookups
{
    public interface IPublishEndpointProvider
    {
        string GetLocationEndpoint(NotificationTopic location);
        string GetLocationName(NotificationTopic location);
    }

    public class SnsPublishEndpointProvider : IPublishEndpointProvider
    {
        public string GetLocationEndpoint(NotificationTopic location)
        {
            // Eventually this should come from the Settings API (having been published somewhere by the create process).
            switch (location)
            {
                case NotificationTopic.OrderDispatch:
                    return "arn:aws:sns:eu-west-1:507204202721:uk-qa12-order-dispatch";

                case NotificationTopic.CustomerCommunication:
                    return "arn:aws:sns:eu-west-1:507204202721:uk-qa12-customer-order-communication";
            } 

            throw new IndexOutOfRangeException("There is no endpoint defined for the provided location type");
        }

        public string GetLocationName(NotificationTopic location)
        {
            // Eventually this should include the environment etc.
            switch (location)
            {
                case NotificationTopic.OrderDispatch:
                    return "order-dispatch";

                case NotificationTopic.CustomerCommunication:
                    return "customer-order-communication";
            }
            
            throw new IndexOutOfRangeException("There is no location defined for the provided location type");
        }
    }
}