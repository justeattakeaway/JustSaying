using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Monitoring;

/// <summary>
/// No-op monitor implementation. Used when no monitoring is configured.
/// </summary>
public class NullKafkaConsumerMonitor : IKafkaConsumerMonitor
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly NullKafkaConsumerMonitor Instance = new();

    private NullKafkaConsumerMonitor() { }

    public void OnMessageReceived<T>(MessageReceivedContext<T> context) where T : Message { }
    public void OnMessageProcessed<T>(MessageProcessedContext<T> context) where T : Message { }
    public void OnMessageFailed<T>(MessageFailedContext<T> context) where T : Message { }
    public void OnMessageDeadLettered<T>(MessageDeadLetteredContext<T> context) where T : Message { }
}

