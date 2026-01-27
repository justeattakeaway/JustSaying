using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Handlers;

/// <summary>
/// Failure handler that forwards messages to a failure/DLT topic.
/// Used for topic chaining mode and as the DLT forwarder in in-process mode.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class KafkaFailureHandler<T> : IFailureHandler<T>, IDisposable
    where T : Message
{
    private readonly string _targetTopic;
    private readonly IProducer<string, byte[]> _producer;
    private readonly ILogger _logger;
    private bool _disposed;

    public KafkaFailureHandler(
        string targetTopic,
        KafkaConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        _targetTopic = targetTopic ?? throw new ArgumentNullException(nameof(targetTopic));
        _logger = loggerFactory.CreateLogger("JustSaying.Kafka.FailureHandler");

        var producerConfig = configuration.GetProducerConfig();
        producerConfig.Acks = Acks.All; // Ensure durability for failure messages

        _producer = new ProducerBuilder<string, byte[]>(producerConfig)
            .SetErrorHandler((_, error) =>
                _logger.LogError("Failure handler producer error: {Reason}", error.Reason))
            .Build();

        _logger.LogDebug("Initialized failure handler for topic '{TargetTopic}'", _targetTopic);
    }

    /// <inheritdoc />
    public async Task OnFailureAsync(
        MessageFailureContext<T> context,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(KafkaFailureHandler<T>));

        try
        {
            // Reset timestamp for delay calculation on retry topics
            var message = new Message<string, byte[]>
            {
                Key = context.KafkaResult.Message.Key,
                Value = context.KafkaResult.Message.Value,
                Headers = context.KafkaResult.Message.Headers ?? new Headers(),
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            // Add error metadata to headers
            AddErrorHeaders(message.Headers, context, exception);

            var result = await _producer
                .ProduceAsync(_targetTopic, message, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogWarning(
                "Message forwarded to '{TargetTopic}' (partition: {Partition}, offset: {Offset}). " +
                "Source: {SourceTopic}:{SourcePartition}:{SourceOffset}. " +
                "Error: {ExceptionType}: {ExceptionMessage}",
                _targetTopic,
                result.Partition.Value,
                result.Offset.Value,
                context.Topic,
                context.Partition,
                context.Offset,
                exception.GetType().Name,
                exception.Message);
        }
        catch (ProduceException<string, byte[]> ex)
        {
            _logger.LogCritical(ex,
                "CRITICAL: Failed to forward message to '{TargetTopic}'. " +
                "Message from {SourceTopic}:{SourcePartition}:{SourceOffset} may be lost!",
                _targetTopic,
                context.Topic,
                context.Partition,
                context.Offset);
            throw;
        }
    }

    private static void AddErrorHeaders(Headers headers, MessageFailureContext<T> context, Exception exception)
    {
        // Add DLT metadata headers
        headers.Add("x-dlt-source-topic", System.Text.Encoding.UTF8.GetBytes(context.Topic ?? string.Empty));
        headers.Add("x-dlt-source-partition", System.Text.Encoding.UTF8.GetBytes(context.Partition.ToString()));
        headers.Add("x-dlt-source-offset", System.Text.Encoding.UTF8.GetBytes(context.Offset.ToString()));
        headers.Add("x-dlt-exception-type", System.Text.Encoding.UTF8.GetBytes(exception.GetType().FullName ?? exception.GetType().Name));
        headers.Add("x-dlt-exception-message", System.Text.Encoding.UTF8.GetBytes(exception.Message ?? string.Empty));
        headers.Add("x-dlt-retry-attempt", System.Text.Encoding.UTF8.GetBytes(context.RetryAttempt.ToString()));
        headers.Add("x-dlt-timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error flushing failure handler producer");
        }

        _producer?.Dispose();
        _disposed = true;
    }
}

