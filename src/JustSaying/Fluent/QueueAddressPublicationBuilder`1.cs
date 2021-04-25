using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    public sealed class QueueAddressPublicationBuilder<T> : IPublicationBuilder<T>
        where T : Message
    {
        private readonly QueueAddress _queueAddress;

        internal QueueAddressPublicationBuilder(QueueAddress queueAddress)
        {
            _queueAddress = queueAddress;
        }

        public void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<TopicAddressPublicationBuilder<T>>();

            logger.LogInformation("Adding SQS publisher for message type '{MessageType}'", typeof(T));

            bus.SerializationRegister.AddSerializer<T>();

            var eventPublisher = new SqsMessagePublisher(
                _queueAddress.QueueUrl,
                proxy.GetAwsClientFactory().GetSqsClient(RegionEndpoint.GetBySystemName(_queueAddress.RegionName)),
                bus.SerializationRegister,
                loggerFactory);

            bus.AddMessagePublisher<T>(eventPublisher);

            logger.LogInformation(
                "Created SQS queue publisher on queue URL '{QueueName}' for message type '{MessageType}'",
                _queueAddress.QueueUrl,
                typeof(T));
        }
    }
}
