using JustSaying.AwsTools.QueueCreation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// Defines a builder for a subscription.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the messages to subscribe to.
    /// </typeparam>
    internal interface ISubscriptionBuilder<out T>
        where T : Message
    {
        /// <summary>
        /// Configures the subscriptions for the <see cref="JustSayingBus"/>.
        /// </summary>
        /// <param name="bus">The <see cref="JustSayingBus"/> to configure subscriptions for.</param>
        /// <param name="handlerResolver">The <see cref="IHandlerResolver"/> to resolve handlers from.</param>
        /// <param name="serviceResolver">The <see cref="IServiceResolver"/> to resolve middleware services from.</param>
        /// <param name="creator">The <see cref="IVerifyAmazonQueues"/> to use to create queues with.</param>
        /// <param name="awsClientFactoryProxy">The <see cref="IAwsClientFactoryProxy"/> to use to create SQS/SNS clients with.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> logger factory to use.</param>
        void Configure(
            JustSayingBus bus,
            IHandlerResolver handlerResolver,
            IServiceResolver serviceResolver,
            IVerifyAmazonQueues creator,
            IAwsClientFactoryProxy awsClientFactoryProxy,
            ILoggerFactory loggerFactory);
    }
}
