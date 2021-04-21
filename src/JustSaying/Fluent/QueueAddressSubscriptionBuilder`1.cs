using System;
using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    public sealed class QueueAddressSubscriptionBuilder<T> : ISubscriptionBuilder<T>
        where T : Message
    {
        private readonly Uri _queueUrl;
        private readonly string _regionName;

        internal QueueAddressSubscriptionBuilder(Uri queueUrl, string regionName)
        {
            _queueUrl = queueUrl;
            _regionName = regionName;
        }

        private Action<QueueAddressConfiguration> ConfigureReads { get; set; }

        public QueueAddressSubscriptionBuilder<T> WithReadConfiguration(Action<QueueAddressConfiguration> configure)
        {
            ConfigureReads = configure ?? throw new ArgumentNullException(nameof(configure));
            return this;
        }

        /// <inheritdoc />
        void ISubscriptionBuilder<T>.Configure(
            JustSayingBus bus,
            IHandlerResolver handlerResolver,
            IServiceResolver serviceResolver,
            IVerifyAmazonQueues creator,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<QueueSubscriptionBuilder<T>>();

            var attachedQueueConfig = new QueueAddressConfiguration();

            ConfigureReads?.Invoke(attachedQueueConfig);

            IAmazonSQS sqsClient = serviceResolver
                .ResolveService<IAwsClientFactory>()
                .GetSqsClient(RegionEndpoint.GetBySystemName(_regionName));

            var queue = new QueueAddressQueue(_queueUrl, sqsClient);

            attachedQueueConfig.SubscriptionGroupName ??= queue.QueueName;
            attachedQueueConfig.Validate();

            bus.AddQueue(attachedQueueConfig.SubscriptionGroupName, queue);

            logger.LogInformation(
                "Created SQS queue subscription for '{MessageType}' on '{QueueName}'",
                typeof(T), queue.QueueName);

            var resolutionContext = new HandlerResolutionContext(queue.QueueName);
            var proposedHandler = handlerResolver.ResolveHandler<T>(resolutionContext);
            if (proposedHandler == null)
            {
                throw new HandlerNotRegisteredWithContainerException(
                    $"There is no handler for '{typeof(T)}' messages.");
            }

            var middlewareBuilder = new HandlerMiddlewareBuilder(handlerResolver, serviceResolver);

            var handlerMiddleware = middlewareBuilder
                .UseHandler<T>()
                .UseStopwatch(proposedHandler.GetType())
                .Configure(attachedQueueConfig.MiddlewareConfiguration)
                .Build();

            bus.AddMessageMiddleware<T>(queue.QueueName, handlerMiddleware);

            logger.LogInformation(
                "Added a message handler for message type for '{MessageType}' on queue '{QueueName}'",
                typeof(T), queue.QueueName);
        }
    }
}
