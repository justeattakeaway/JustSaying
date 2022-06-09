using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

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
    /// </summary>
    /// <param name="middlewareConfiguration">A configuration action that provides a way
    /// to override the default middleware behaviour. By default, this builder calls <see cref="HandlerMiddlewareBuilderExtensions.UseDefaults{TMessage}"/> which applies a set of
    /// default middlewares to add metrics, error handling, completion handling, context setting, and logging.
    /// </param>
    /// <example>
    /// A sample configuration with additional middlewares:
    /// <code>
    /// WithMiddlewareConfiguration(pipe =>
    /// {
    ///     pipe.Use&lt;SomeCustomMiddleware&gt;();
    ///     pipe.Use&lt;SomeOtherCustomMiddleware&gt;();
    ///     pipe.UseDefaults&lt;SimpleMessage&gt;(typeof(MyHandler));
    /// });
    /// </code>
    /// would yield this order of execution:
    /// <ul>
    /// <li>Before_SomeCustomMiddleware</li>
    /// <li>Before_SomeOtherCustomMiddleware</li>
    /// <li>Before_MessageContextAccessorMiddleware</li>
    /// <li>Before_LoggingMiddleware</li>
    /// <li>Before_StopwatchMiddleware</li>
    /// <li>Before_SqsPostProcessorMiddleware</li>
    /// <li>Before_ErrorHandlerMiddleware</li>
    /// <li>Before_HandlerInvocationMiddleware</li>
    /// <li>After_HandlerInvocationMiddleware</li>
    /// <li>After_ErrorHandlerMiddleware</li>
    /// <li>After_SqsPostProcessorMiddleware</li>
    /// <li>After_StopwatchMiddleware</li>
    /// <li>After_LoggingMiddleware</li>
    /// <li>After_MessageContextAccessorMiddleware</li>
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
