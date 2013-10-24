using System;
using System.Globalization;

namespace JustEat.Simples.NotificationStack.Messaging.Lookups
{
    public interface IPublishSubscribtionEndpointProvider
    {
        string GetLocationEndpoint(string component, string topic);
        string GetLocationName(string component, string topic);
        string GetLocationName(string component, string topic, int instancePosition);
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

        public string GetLocationEndpoint(string component, string topic)
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

        public string GetLocationName(string component, string topic)
        {
            return GetLocationNameInternal(component, topic);
        }

        public string GetLocationName(string component, string topic, int instancePosition)
        {
            return GetLocationNameInternal(component, topic, instancePosition);
        }

        private string GetLocationNameInternal(string component, string topic, int? instancePosition = null)
        {
            if (instancePosition.HasValue && instancePosition.Value <= 0)
                throw new ArgumentOutOfRangeException("instancePosition", "Cannot have an instance position less than 1. Check your configuration.");

            var instancePositionValue = instancePosition.HasValue
                                            ? instancePosition.Value.ToString(CultureInfo.InvariantCulture)
                                            : string.Empty;

            return String.Join("-", new[] { _config.Tenant, _config.Environment, component, instancePositionValue, topic }).ToLower().Replace("--", "-");
        }
    }
}