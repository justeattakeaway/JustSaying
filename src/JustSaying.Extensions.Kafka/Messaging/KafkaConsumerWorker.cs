using JustSaying.Extensions.Kafka.Attributes;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Monitoring;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Messaging;

#if NETSTANDARD2_0 || NET462
/// <summary>
/// Polyfill for BackgroundService for older target frameworks.
/// </summary>
public abstract class BackgroundServiceBase : IHostedService, IDisposable
{
    private Task _executingTask;
    private CancellationTokenSource _stoppingCts;

    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_stoppingCts.Token);

        if (_executingTask.IsCompleted)
        {
            return _executingTask;
        }

        return Task.CompletedTask;
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask == null)
        {
            return;
        }

        try
        {
            _stoppingCts?.Cancel();
        }
        finally
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
        }
    }

    public virtual void Dispose()
    {
        _stoppingCts?.Cancel();
        _stoppingCts?.Dispose();
    }
}
#endif

/// <summary>
/// Background service that runs a Kafka consumer.
/// Automatically starts consuming when the host starts and stops gracefully on shutdown.
/// </summary>
/// <typeparam name="T">The message type to consume.</typeparam>
[IgnoreKafkaInWarmUp]
#if NETSTANDARD2_0 || NET462
public class KafkaConsumerWorker<T> : BackgroundServiceBase where T : Message
#else
public class KafkaConsumerWorker<T> : BackgroundService where T : Message
#endif
{
    private readonly string _consumerId;
    private readonly string _topic;
    private readonly KafkaConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private KafkaMessageConsumer _consumer;

    /// <summary>
    /// Creates a new Kafka consumer worker.
    /// </summary>
    /// <param name="consumerId">Unique identifier for this consumer instance.</param>
    /// <param name="topic">The topic to consume from.</param>
    /// <param name="configuration">The Kafka configuration.</param>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public KafkaConsumerWorker(
        string consumerId,
        string topic,
        KafkaConfiguration configuration,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        _consumerId = consumerId ?? throw new ArgumentNullException(nameof(consumerId));
        _topic = topic ?? throw new ArgumentNullException(nameof(topic));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = loggerFactory?.CreateLogger($"JustSaying.Kafka.Worker.{consumerId}")
            ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield to allow startup to complete before we start consuming
        await Task.Yield();

        _logger.LogInformation(
            "[Worker {ConsumerId}] Starting consumer for topic '{Topic}'",
            _consumerId, _topic);

        try
        {
            using var scope = _serviceProvider.CreateScope();

            // Resolve dependencies
            var serializationFactory = scope.ServiceProvider.GetRequiredService<IMessageBodySerializationFactory>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var monitor = scope.ServiceProvider.GetKafkaConsumerMonitor();
            var contextAccessor = scope.ServiceProvider.GetKafkaMessageContextAccessor();

            // Create consumer
            _consumer = new KafkaMessageConsumer(
                _topic,
                _configuration,
                serializationFactory,
                loggerFactory,
                monitor,
                contextAccessor);

            // Resolve handler
            var handler = scope.ServiceProvider.GetRequiredService<IHandlerAsync<T>>();

            _logger.LogInformation(
                "[Worker {ConsumerId}] Consumer started, listening on topic '{Topic}'",
                _consumerId, _topic);

            // Start consuming
            await _consumer.StartAsync(handler, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "[Worker {ConsumerId}] Consumer stopped gracefully",
                _consumerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Worker {ConsumerId}] Consumer crashed unexpectedly",
                _consumerId);
            throw;
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Worker {ConsumerId}] Stopping consumer...",
            _consumerId);

        if (_consumer != null)
        {
            await _consumer.StopAsync().ConfigureAwait(false);
            _consumer.Dispose();
            _consumer = null;
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "[Worker {ConsumerId}] Consumer stopped",
            _consumerId);
    }
}

