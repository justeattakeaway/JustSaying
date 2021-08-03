using System;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.Middleware;
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
    public interface ISubscriptionBuilder<out T>
        where T : Message
    {
        /// <summary>
        /// Configures the middleware pipeline for this subscription.
        /// Any middleware configured here will be wrapped around a handler and metrics middleware.
        /// </summary>
        /// <param name="middlewareConfiguration"></param>
        /// <example>
        /// A sample configuration:
        /// <code>
        /// WithMiddlewareConfiguration(pipe =>
        /// {
        ///     pipe.Use&lt;SomeCustomMiddleware&gt;();
        ///     pipe.Use&lt;SomeOtherCustomMiddleware&gt;();
        /// });
        /// </code>
        /// would yield this order of execution:
        /// <ul>
        /// <li>Before_SomeCustomMiddleware</li>
        /// <li>Before_SomeOtherCustomMiddleware</li>
        /// <li>Before_StopwatchMiddleware</li>
        /// <li>Before_HandlerInvocationMiddleware</li>
        /// <li>After_HandlerInvocationMiddleware</li>
        /// <li>After_StopwatchMiddleware</li>
        /// <li>After_SomeOtherCustomMiddleware</li>
        /// <li>After_SomeCustomMiddleware</li>
        /// </ul>
        /// </example>
        /// <returns>The current <see cref="SqsReadConfigurationBuilder"/>.</returns>
        public ISubscriptionBuilder<T> WithMiddlewareConfiguration(Action<HandlerMiddlewareBuilder> middlewareConfiguration);


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
