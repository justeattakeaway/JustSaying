using JustSaying.Messaging.Channels.Dispatch;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Interrogation;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.SubscriptionGroups;

/// <inheritdoc />
/// <summary>
/// Coordinates reading messages from a collection of <see cref="IMessageReceiveBuffer"/>
/// and dispatching using a collection of <see cref="IMultiplexerSubscriber"/>.
/// </summary>
/// <param name="settings">The <see cref="SubscriptionGroupSettings"/> to use.</param>
/// <param name="receiveBuffers">The collection of <see cref="IMessageReceiveBuffer"/> to read from.</param>
/// <param name="multiplexer">The <see cref="IMultiplexer"/> to aggregate all messages into one stream.</param>
/// <param name="subscribers">The collection of <see cref="IMultiplexerSubscriber"/> that will dispatch the messages</param>
/// <param name="logger">The <see cref="ILogger"/> to be used.</param>
internal class SubscriptionGroup(
    SubscriptionGroupSettings settings,
    ICollection<IMessageReceiveBuffer> receiveBuffers,
    IMultiplexer multiplexer,
    ICollection<IMultiplexerSubscriber> subscribers,
    ILogger<SubscriptionGroup> logger) : ISubscriptionGroup
{
    /// <inheritdoc />
    public Task RunAsync(CancellationToken stoppingToken)
    {
        var receiveBufferQueueNames = string.Join(",", receiveBuffers.Select(rb => rb.QueueName));

        logger.LogInformation(
            "Starting up SubscriptionGroup {SubscriptionGroupName} for queues [{Queues}] with {ReceiveBufferCount} receive buffers and {SubscriberCount} subscribers.",
            settings.Name,
            receiveBufferQueueNames,
            receiveBuffers.Count,
            subscribers.Count);

        var completionTasks = new List<Task>
        {
            multiplexer.RunAsync(stoppingToken)
        };
        completionTasks.AddRange(subscribers.Select(subscriber => subscriber.RunAsync(stoppingToken)));
        completionTasks.AddRange(receiveBuffers.Select(buffer => buffer.RunAsync(stoppingToken)));

        return Task.WhenAll(completionTasks);
    }

    /// <inheritdoc />
    public InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {
            settings.Name,
            ConcurrencyLimit = subscribers.Count,
            Multiplexer = multiplexer.Interrogate(),
            ReceiveBuffers = receiveBuffers.Select(rb => rb.Interrogate()).ToArray(),
        });
    }
}
