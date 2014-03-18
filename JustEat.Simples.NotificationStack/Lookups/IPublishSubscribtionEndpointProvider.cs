using System;
using System.Globalization;
using JustEat.Simples.NotificationStack.Messaging;

namespace JustEat.Simples.NotificationStack.Stack.Lookups
{
    public interface IPublishSubscribtionEndpointProvider
    {
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