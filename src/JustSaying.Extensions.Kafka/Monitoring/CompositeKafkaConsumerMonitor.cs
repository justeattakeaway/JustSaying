using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Monitoring;

/// <summary>
/// Aggregates multiple monitors and invokes them all.
/// This allows registering multiple monitoring implementations.
/// </summary>
internal class CompositeKafkaConsumerMonitor : IKafkaConsumerMonitor
{
    private readonly IEnumerable<IKafkaConsumerMonitor> _monitors;

    public CompositeKafkaConsumerMonitor(IEnumerable<IKafkaConsumerMonitor> monitors)
    {
        _monitors = monitors ?? Enumerable.Empty<IKafkaConsumerMonitor>();
    }

    public void OnMessageReceived<T>(MessageReceivedContext<T> context) where T : Message
    {
        foreach (var monitor in _monitors)
        {
            try
            {
                monitor.OnMessageReceived(context);
            }
            catch
            {
                // Don't let monitoring failures affect message processing
            }
        }
    }

    public void OnMessageProcessed<T>(MessageProcessedContext<T> context) where T : Message
    {
        foreach (var monitor in _monitors)
        {
            try
            {
                monitor.OnMessageProcessed(context);
            }
            catch
            {
                // Don't let monitoring failures affect message processing
            }
        }
    }

    public void OnMessageFailed<T>(MessageFailedContext<T> context) where T : Message
    {
        foreach (var monitor in _monitors)
        {
            try
            {
                monitor.OnMessageFailed(context);
            }
            catch
            {
                // Don't let monitoring failures affect message processing
            }
        }
    }

    public void OnMessageDeadLettered<T>(MessageDeadLetteredContext<T> context) where T : Message
    {
        foreach (var monitor in _monitors)
        {
            try
            {
                monitor.OnMessageDeadLettered(context);
            }
            catch
            {
                // Don't let monitoring failures affect message processing
            }
        }
    }
}

