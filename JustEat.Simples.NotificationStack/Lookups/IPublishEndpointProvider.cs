using System;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Lookups;

namespace JustSaying.Stack.Lookups
{

    public class SnsPublishEndpointProvider : IPublishEndpointProvider
    {
        private readonly IMessagingConfig _publisherConfig;
        private readonly SqsConfiguration _subscriptionConfig;

        public SnsPublishEndpointProvider(IMessagingConfig publisherConfig, SqsConfiguration subscriptionConfig)
        {
            _publisherConfig = publisherConfig;
            _subscriptionConfig = subscriptionConfig;
        }

        public string GetLocationName(string location)
        {
            return String.Join("-", new[] { _publisherConfig.Tenant, _publisherConfig.Environment, location }).ToLower();
        }

        public string GetLocationName()
        {
            return String.Join("-", new[] { _publisherConfig.Tenant, _publisherConfig.Environment, _subscriptionConfig.Topic }).ToLower();
        }
    }
}