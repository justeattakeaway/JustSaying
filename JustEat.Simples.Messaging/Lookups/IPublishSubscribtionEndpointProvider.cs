using System;

namespace JustEat.Simples.NotificationStack.Messaging.Lookups
{
    public interface IPublishSubscribtionEndpointProvider
    {
        string GetLocationEndpoint(string component, NotificationTopic topic);
        string GetLocationName(string component, NotificationTopic topic);
    }

    /// <summary>
    /// Provides endpoint locations for SQS queues subscribed to topics
    /// </summary>
    public class SqsSubscribtionEndpointProvider : IPublishSubscribtionEndpointProvider
    {
        private readonly IMessagingConfig _config;

        public SqsSubscribtionEndpointProvider(IMessagingConfig config)
        {
            _config = config;
        }

        public string GetLocationEndpoint(string component, NotificationTopic topic)
        {
            throw new NotImplementedException("Not implemented yet. Come back later");
            //switch (component)
            //{
            //    case Component.OrderEngine:
            //        if (topic == NotificationTopic.CustomerCommunication)
            //            return "https://sqs.eu-west-1.amazonaws.com/507204202721/uk-qa12-orderengine-customer-order-communication";
            //        break;
            //    case Component.SmsSender:
            //        if (topic == NotificationTopic.OrderDispatch)
            //            return "https://sqs.eu-west-1.amazonaws.com/507204202721/uk-qa12-sms-send-order-dispatch";
            //        break;
            //}

            //throw new IndexOutOfRangeException(string.Format("Cannot map an endpoint for component '{0}' and topic '{1}'", component.ToString(), topic.ToString()));
        }

        public string GetLocationName(string component, NotificationTopic topic)
        {
            return String.Join("-", new[] { _config.Tenant, _config.Environment, component, topic.ToString() }).ToLower();
        }
    }
}