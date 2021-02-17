using System.Collections.Generic;
using Amazon;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.Publishing
{
    public interface IQueueTopicCreatorFactory
    {
        ITopicCreator CreateSnsCreator(string topicName, Dictionary<string, string> tags);
        IQueueCreator CreateSqsCreator(string queueName,
            string region,
            int retryCountBeforeSendingToErrorQueue,
            Dictionary<string, string> tags);
    }

    internal class QueueTopicCreatorFactory : IQueueTopicCreatorFactory
    {
        private readonly IAwsClientFactoryProxy _proxy;
        private readonly IMessageSerializationRegister _messageSerializationRegister;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMessagingConfig _config;

        public QueueTopicCreatorFactory(
            IAwsClientFactoryProxy proxy,
            IMessageSerializationRegister messageSerializationRegister,
            ILoggerFactory loggerFactory,
            IMessagingConfig config)
        {
            _proxy = proxy;
            _messageSerializationRegister = messageSerializationRegister;
            _loggerFactory = loggerFactory;
            _config = config;
        }

        public ITopicCreator CreateSnsCreator(string topicName, Dictionary<string, string> tags)
        {
            throw new System.NotImplementedException();
        }

        public IQueueCreator CreateSqsCreator(
            string queueName,
            string region,
            int retryCountBeforeSendingToErrorQueue,
            Dictionary<string, string> tags)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var client = _proxy.GetAwsClientFactory().GetSqsClient(regionEndpoint);

            var queueCreator = new SqsPublisher(regionEndpoint, queueName, client, retryCountBeforeSendingToErrorQueue, _messageSerializationRegister, _loggerFactory)
            {
                MessageResponseLogger = _config.MessageResponseLogger
            };

            return queueCreator;
        }
    }
}
