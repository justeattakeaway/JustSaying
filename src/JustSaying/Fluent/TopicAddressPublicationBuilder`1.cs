using System;
using Amazon;
using JustSaying.AwsTools;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    public sealed class TopicAddressPublicationBuilder<T> : IPublicationBuilder<T>
        where T : Message
    {
        private readonly string _topicArn;
        private Func<Exception,Message,bool> _exceptionHandler = null;


        public TopicAddressPublicationBuilder<T> WithExceptionHandler(Func<Exception, Message, bool> exceptionHandler)
        {
            _exceptionHandler = exceptionHandler;
            return this;
        }

        internal TopicAddressPublicationBuilder(TopicAddress topicAddress)
        {
            _topicArn = topicAddress.TopicArn;
        }

        public void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<TopicAddressPublicationBuilder<T>>();

            logger.LogInformation("Adding SNS publisher for message type '{MessageType}'", typeof(T));

            var config = bus.Config;
            var arn = Arn.Parse(_topicArn);

            bus.SerializationRegister.AddSerializer<T>();

            var eventPublisher = new TopicAddressPublisher(
                proxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(arn.Region)),
                loggerFactory,
                config.MessageSubjectProvider,
                bus.SerializationRegister,
                _exceptionHandler,
                _topicArn);
            bus.AddMessagePublisher<T>(eventPublisher);

            logger.LogInformation(
                "Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'",
                arn.Resource,
                typeof(T));
        }
    }
}
