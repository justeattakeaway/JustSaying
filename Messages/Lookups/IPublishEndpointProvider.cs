using System;

namespace SimplesNotificationStack.Messaging.Lookups
{
    public interface IPublishEndpointProvider
    {
        string GetLocationEndpoint(PublishLocation location);
        string GetLocationName(PublishLocation location);
    }

    public class SnsPublishEndpointProvider : IPublishEndpointProvider
    {
        public string GetLocationEndpoint(PublishLocation location)
        {
            // Eventually this should come from the Settings API (having been published somewhere by the create process).
            switch (location)
            {
                case PublishLocation.OrderDispatch:
                    return "arn:aws:sns:eu-west-1:507204202721:uk-qa12-order-dispatch";

                case PublishLocation.CustomerCommunication:
                    return "arn:aws:sns:eu-west-1:507204202721:uk-qa12-customer-order-communication";
            } 

            throw new IndexOutOfRangeException("There is no endpoint defined for the provided location type");
        }

        public string GetLocationName(PublishLocation location)
        {
            // Eventually this should include the environment etc.
            switch (location)
            {
                case PublishLocation.OrderDispatch:
                    return "order-dispatch";

                case PublishLocation.CustomerCommunication:
                    return "customer-order-communication";
            }
            
            throw new IndexOutOfRangeException("There is no location defined for the provided location type");
        }
    }

    public enum PublishLocation { CustomerCommunication, OrderDispatch }
}
