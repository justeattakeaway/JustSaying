using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Lookups
{
    /// <summary>
    /// Provides endpoint locations for SQS queues subscribed to topics
    /// </summary>
    public class SqsSubscribtionEndpointProvider : IPublishSubscribtionEndpointProvider
    {
        private readonly SqsReadConfiguration _config;

        public SqsSubscribtionEndpointProvider(SqsReadConfiguration config)
        {
            _config = config;
        }

        // ToDo: Add validate to this?
        public string GetLocationName()
        {
            return _config.QueueName.ToLower();
        }
    }
}