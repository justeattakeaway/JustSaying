using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Streams;

/// <summary>
/// Extension methods for configuring Kafka stream processing.
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Creates a new Kafka stream from a source topic.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="sourceTopic">The source topic to consume from.</param>
    /// <returns>A stream builder for fluent configuration.</returns>
    public static KafkaStreamBuilder<T> Stream<T>(string sourceTopic) where T : Message
    {
        return new KafkaStreamBuilder<T>(sourceTopic);
    }

    /// <summary>
    /// Registers a Kafka stream processor as a hosted service.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for the stream.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafkaStream<T>(
        this IServiceCollection services,
        Action<KafkaStreamBuilder<T>> configure) where T : Message
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        // We need to get the source topic from the builder
        // Create a temporary builder to extract configuration
        var tempBuilder = new KafkaStreamBuilder<T>("temp");
        // Note: The actual topic will be set by the configure action

        services.AddSingleton<IHostedService>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var serializationFactory = sp.GetRequiredService<IMessageBodySerializationFactory>();
            var publisher = sp.GetService<IMessagePublisher>();

            return new KafkaStreamWorker<T>(configure, serializationFactory, loggerFactory, publisher);
        });

        return services;
    }

    /// <summary>
    /// Registers a Kafka stream processor with explicit topic configuration.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="sourceTopic">The source topic.</param>
    /// <param name="configure">Configuration action for the stream.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafkaStream<T>(
        this IServiceCollection services,
        string sourceTopic,
        Action<KafkaStreamBuilder<T>> configure) where T : Message
    {
        if (string.IsNullOrEmpty(sourceTopic))
            throw new ArgumentException("Source topic is required", nameof(sourceTopic));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        services.AddSingleton<IHostedService>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var serializationFactory = sp.GetRequiredService<IMessageBodySerializationFactory>();
            var publisher = sp.GetService<IMessagePublisher>();

            return new KafkaStreamWorker<T>(
                sourceTopic,
                configure,
                serializationFactory,
                loggerFactory,
                publisher);
        });

        return services;
    }
}

/// <summary>
/// Background service that runs a Kafka stream processor.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class KafkaStreamWorker<T> :
#if NET6_0_OR_GREATER
    BackgroundService
#else
    BackgroundServiceBase
#endif
    where T : Message
{
    private readonly string _sourceTopic;
    private readonly Action<KafkaStreamBuilder<T>> _configure;
    private readonly IMessageBodySerializationFactory _serializationFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger _logger;

    private KafkaMessageConsumer _consumer;
    private StreamHandler<T> _handler;

    public KafkaStreamWorker(
        Action<KafkaStreamBuilder<T>> configure,
        IMessageBodySerializationFactory serializationFactory,
        ILoggerFactory loggerFactory,
        IMessagePublisher publisher)
    {
        _configure = configure;
        _serializationFactory = serializationFactory;
        _loggerFactory = loggerFactory;
        _publisher = publisher;
        _logger = loggerFactory.CreateLogger<KafkaStreamWorker<T>>();

        // Build to extract source topic
        var builder = new KafkaStreamBuilder<T>("stream");
        configure(builder);
        _sourceTopic = builder.GetSourceTopic();
    }

    public KafkaStreamWorker(
        string sourceTopic,
        Action<KafkaStreamBuilder<T>> configure,
        IMessageBodySerializationFactory serializationFactory,
        ILoggerFactory loggerFactory,
        IMessagePublisher publisher)
    {
        _sourceTopic = sourceTopic;
        _configure = configure;
        _serializationFactory = serializationFactory;
        _loggerFactory = loggerFactory;
        _publisher = publisher;
        _logger = loggerFactory.CreateLogger<KafkaStreamWorker<T>>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        _logger.LogInformation(
            "[StreamWorker] Starting stream processor for topic '{Topic}'",
            _sourceTopic);

        try
        {
            // Build the stream
            var builder = new KafkaStreamBuilder<T>(_sourceTopic);
            _configure(builder);

            _handler = builder.Build();
            _handler.Initialize(_publisher, _loggerFactory);

            var configuration = builder.GetConfiguration();
            _consumer = new KafkaMessageConsumer(
                _sourceTopic,
                configuration,
                _serializationFactory,
                _loggerFactory);

            await _consumer.StartAsync(_handler, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("[StreamWorker] Stream processor stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StreamWorker] Stream processor crashed");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[StreamWorker] Stopping stream processor");

        if (_consumer != null)
        {
            await _consumer.StopAsync().ConfigureAwait(false);
            _consumer.Dispose();
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}

#if NETSTANDARD2_0 || NET462
/// <summary>
/// Polyfill for BackgroundService for older target frameworks.
/// </summary>
internal abstract class BackgroundServiceBase : IHostedService, IDisposable
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
