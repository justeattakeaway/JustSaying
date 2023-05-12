using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware.Receive;
using ReceiveMiddleware =
    JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.Receive.ReceiveMessagesContext,
        System.Collections.Generic.IList<Amazon.SQS.Model.Message>>;

namespace JustSaying.Messaging.Channels.SubscriptionGroups;

/// <summary>
/// Configures the default settings for all subscription groups.
/// </summary>
public class SubscriptionGroupSettingsBuilder : ISubscriptionGroupSettings
{
    public SubscriptionGroupSettingsBuilder()
    {
        BufferSize = MessageDefaults.MaxAmazonMessageCap;
        ReceiveBufferReadTimeout = TimeSpan.FromMinutes(5);
        ReceiveMessagesWaitTime = TimeSpan.FromSeconds(20);
        MultiplexerCapacity = 100;
        Prefetch = 10;
        ConcurrencyLimit = Environment.ProcessorCount * MessageDefaults.ParallelHandlerExecutionPerCore;
    }

    /// <summary>
    /// Gets the default number of messages to try and fetch from SQS per attempt for each queue.
    /// </summary>
    public int Prefetch { get; private set; }

    /// <summary>
    /// Gets the default number of messages that will be buffered from SQS for each of the queues.
    /// </summary>
    public int BufferSize { get; private set; }

    /// <summary>
    /// Gets the default maximum amount of time to wait for messages to be available on each SQS queue in a
    /// <see cref="ISubscriptionGroup"/> before resetting the connection.
    /// </summary>
    public TimeSpan ReceiveBufferReadTimeout { get; private set; }

    /// <summary>
    /// Gets the default duration SQS will wait for a message before returning if there are no messages.
    /// </summary>
    public TimeSpan ReceiveMessagesWaitTime { get; private set; }

    /// <summary>
    /// Gets the default maximum number of messages that may be processed at once by a <see cref="ISubscriptionGroup"/>.
    /// </summary>
    public int ConcurrencyLimit { get; private set; }

    /// <summary>
    /// Gets the default number of messages that may be buffered across all of the queues in this <see cref="ISubscriptionGroup"/>.
    /// </summary>
    public int MultiplexerCapacity { get; private set; }

    /// <summary>
    /// Gets the default <see cref="ReceiveMiddleware"/> to be used by the receive pipeline.
    /// </summary>
    public ReceiveMiddleware SqsMiddleware { get; private set; }

    /// <summary>
    /// Specifies the default maximum amount of time to wait for messages to be available on each SQS queue in a
    /// <see cref="ISubscriptionGroup"/> before resetting the connection.
    /// Defaults to 5 minutes.
    /// </summary>
    /// <param name="receiveBufferReadTimeout">The maximum amount of time to wait to read from each SQS queue.</param>
    /// <returns>This builder object.</returns>
    public SubscriptionGroupSettingsBuilder WithDefaultReceiveBufferReadTimeout(
        TimeSpan receiveBufferReadTimeout)
    {
        ReceiveBufferReadTimeout = receiveBufferReadTimeout;
        return this;
    }

    /// <summary>
    /// Specifies the default duration SQS will wait for a message before returning if there are no messages.
    /// Defaults to 20 seconds, which is the maximum.
    /// </summary>
    /// <param name="waitTime">The maximum amount of time SQS should wait before returning.</param>
    /// <returns>This builder object.</returns>
    public SubscriptionGroupSettingsBuilder WithDefaultReceiveMessagesWaitTime(TimeSpan waitTime)
    {
        ReceiveMessagesWaitTime = waitTime;
        return this;
    }

    /// <summary>
    /// Specifies the default number of messages that may be buffered across all of the queues in this <see cref="ISubscriptionGroup"/>.
    /// Note: This setting is shared across all queues in this group. For per-queue settings, see <see cref="WithDefaultBufferSize"/>
    /// Defaults to 100.
    /// </summary>
    /// <param name="multiplexerCapacity">The maximum multiplexer capacity.</param>
    /// <returns>This builder object.</returns>
    public SubscriptionGroupSettingsBuilder WithDefaultMultiplexerCapacity(int multiplexerCapacity)
    {
        MultiplexerCapacity = multiplexerCapacity;
        return this;
    }

    /// <summary>
    /// Specifies the default number of messages to try and fetch from SQS per attempt for each queue in a <see cref="ISubscriptionGroup"/>.
    /// Defaults to 10.
    /// </summary>
    /// <param name="prefetch">the number of messages to load per request.</param>
    /// <returns>This builder object.</returns>
    public SubscriptionGroupSettingsBuilder WithDefaultPrefetch(int prefetch)
    {
        Prefetch = prefetch;
        return this;
    }

    /// <summary>
    /// Specifies the default maximum number of messages that may be processed at once by a <see cref="ISubscriptionGroup"/>.
    /// Defaults to 4 times the value of <see cref="Environment.ProcessorCount"/>.
    /// </summary>
    /// <param name="concurrencyLimit">The maximum number of messages to process at the same time.</param>
    /// <returns>This builder object.</returns>
    public SubscriptionGroupSettingsBuilder WithDefaultConcurrencyLimit(int concurrencyLimit)
    {
        ConcurrencyLimit = concurrencyLimit;
        return this;
    }

    /// <summary>
    /// Specifies the default number of messages that will be buffered from SQS for each of the queues in a <see cref="ISubscriptionGroup"/>
    /// before waiting for them to drain into the <see cref="IMultiplexer"/>.
    /// Note: This setting is per-queue. To set the shared buffer size for all queues, see <see cref="WithDefaultMultiplexerCapacity"/>.
    /// </summary>
    /// <param name="bufferSize">The maximum number of messages for each queue to buffer.</param>
    /// <returns>This builder object.</returns>
    public SubscriptionGroupSettingsBuilder WithDefaultBufferSize(int bufferSize)
    {
        BufferSize = bufferSize;
        return this;
    }

    /// <summary>
    /// Overrides the default middleware used by the receive pipeline, which performs some default error handling
    /// (see <see cref="DefaultReceiveMessagesMiddleware"/>).
    /// </summary>
    /// <param name="middleware">A <see cref="ReceiveMiddleware"/> that replaces the default middleware
    /// (see <see cref="DefaultReceiveMessagesMiddleware"/>).</param>
    /// <returns>The builder object.</returns>
    public SubscriptionGroupSettingsBuilder WithCustomMiddleware(ReceiveMiddleware middleware)
    {
        SqsMiddleware = middleware;
        return this;
    }
}
