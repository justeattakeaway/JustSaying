using JustSaying.Extensions.Kafka.Fluent;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying;

/// <summary>
/// Service to manage Kafka consumer subscriptions.
/// </summary>
public class KafkaSubscriptionManager
{
    private readonly List<KafkaConsumerRegistration> _consumers = new();

    /// <summary>
    /// Starts all registered Kafka consumers.
    /// </summary>
    public async Task StartAllAsync(CancellationToken cancellationToken)
    {
        var tasks = _consumers.Select(c => c.StartAction(cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Registers a Kafka subscription.
    /// </summary>
    public void RegisterSubscription<T>(IServiceProvider serviceProvider, KafkaSubscriptionBuilder<T> builder) where T : Message
    {
        var consumer = builder.CreateConsumer(serviceProvider);
        var handler = builder.GetHandler(serviceProvider);

        _consumers.Add(new KafkaConsumerRegistration
        {
            Consumer = consumer,
            StartAction = async (ct) => await consumer.StartAsync(handler, ct)
        });
    }

    /// <summary>
    /// Disposes all consumers.
    /// </summary>
    public void Dispose()
    {
        foreach (var registration in _consumers)
        {
            registration.Consumer?.Dispose();
        }
        _consumers.Clear();
    }

    private class KafkaConsumerRegistration
    {
        public KafkaMessageConsumer Consumer { get; set; }
        public Func<CancellationToken, Task> StartAction { get; set; }
    }
}
