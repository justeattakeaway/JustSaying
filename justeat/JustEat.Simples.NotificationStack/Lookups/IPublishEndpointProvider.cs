using System;
using JustSaying.Messaging;

namespace JustSaying.Stack.Lookups
{
    public interface IPublishEndpointProvider
    {
        string GetLocationName(string location);
    }

    public class SnsPublishEndpointProvider : IPublishEndpointProvider
    {
        private readonly IMessagingConfig _config;

        public SnsPublishEndpointProvider(IMessagingConfig config)
        {
            _config = config;
        }

        public string GetLocationName(string location)
        {
            return String.Join("-", new[] { _config.Tenant, _config.Environment, location }).ToLower();
        }
    }
}