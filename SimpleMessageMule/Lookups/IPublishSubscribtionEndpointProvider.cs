using System;
using System.Globalization;
using JustEat.Simples.NotificationStack.AwsTools.QueueCreation;

namespace SimpleMessageMule.Lookups
{
    public interface IPublishSubscribtionEndpointProvider
    {
        string GetLocationName();
    }

    /// <summary>
    /// Provides endpoint locations for SQS queues subscribed to topics
    /// </summary>
    public class SqsSubscribtionEndpointProvider : IPublishSubscribtionEndpointProvider
    {
        private readonly SqsConfiguration _config;

        public SqsSubscribtionEndpointProvider(SqsConfiguration config)
        {
            _config = config;
        }

        public string GetLocationName()
        {
            return _config.Topic.ToLower();
        }
    }
}