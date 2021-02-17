using System.Collections.Generic;
using Amazon;
using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.Publishing
{
    internal class MessagePublisherFactory : IMessagePublisherFactory
    {
        private readonly IAwsClientFactoryProxy _proxy;
        private readonly IMessageSerializationRegister _serializationRegister;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMessagingConfig _config;

        public MessagePublisherFactory(
            IAwsClientFactoryProxy proxy,
            IMessageSerializationRegister serializationRegister,
            ILoggerFactory loggerFactory,
            IMessagingConfig config)
        {
            _proxy = proxy;
            _serializationRegister = serializationRegister;
            _loggerFactory = loggerFactory;
            _config = config;
        }

        public IMessagePublisher CreateSnsPublisher(string topicName, bool throwOnPublishFailure)
        {
            return new SnsTopicByName(topicName,
                _proxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(_config.Region)),
                _serializationRegister,
                _loggerFactory,
                _config.MessageSubjectProvider,
                null,
                throwOnPublishFailure);
        }

        public IMessagePublisher CreateSqsPublisher(string queueName)
        {
            var region = RegionEndpoint.GetBySystemName(_config.Region);
            var client = _proxy.GetAwsClientFactory().GetSqsClient(region);
            return new SqsPublisher(region, queueName, client, 0, _serializationRegister, _loggerFactory);
        }
    }
}
