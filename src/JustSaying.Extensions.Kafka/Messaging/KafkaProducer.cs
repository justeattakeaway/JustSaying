using System.Diagnostics;
using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Attributes;
using JustSaying.Extensions.Kafka.CloudEvents;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Factory;
using JustSaying.Extensions.Kafka.Tracing;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Messaging;

/// <summary>
/// Typed Kafka producer implementation.
/// </summary>
/// <typeparam name="TProducerType">A marker type to identify this producer configuration.</typeparam>
[IgnoreKafkaInWarmUp]
public class KafkaProducer<TProducerType> : IKafkaProducer<TProducerType>
    where TProducerType : class
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly CloudEventsMessageConverter _converter;
    private readonly KafkaConfiguration _configuration;
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Creates a new typed Kafka producer.
    /// </summary>
    public KafkaProducer(
        KafkaConfiguration configuration,
        IKafkaProducerFactory factory,
        IMessageBodySerializationFactory serializationFactory,
        ILoggerFactory loggerFactory)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = loggerFactory?.CreateLogger($"JustSaying.Kafka.Producer.{typeof(TProducerType).Name}")
            ?? throw new ArgumentNullException(nameof(loggerFactory));

        _producer = factory?.CreateProducer(configuration)
            ?? throw new ArgumentNullException(nameof(factory));

        _converter = new CloudEventsMessageConverter(
            serializationFactory ?? throw new ArgumentNullException(nameof(serializationFactory)),
            configuration.CloudEventsSource);
    }

    /// <inheritdoc />
    public async Task<bool> ProduceAsync<TMessage>(
        string topic,
        TMessage message,
        string key = null,
        PublishMetadata metadata = null,
        CancellationToken cancellationToken = default) where TMessage : Message
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic is required", nameof(topic));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageKey = key ?? message.Id.ToString();

        // Start produce activity for distributed tracing
        using var activity = KafkaActivitySource.StartProduceActivity(
            topic,
            message.Id.ToString(),
            messageKey);

        try
        {
            var kafkaMessage = CreateKafkaMessage(message, key, metadata);

            // Inject trace context into headers
            if (activity != null && kafkaMessage.Headers != null)
            {
                TraceContextPropagator.InjectTraceContext(kafkaMessage.Headers, activity);
            }

            var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken)
                .ConfigureAwait(false);

            activity?.SetTag(KafkaActivitySource.MessagingKafkaPartitionTag, result.Partition.Value);
            activity?.SetTag(KafkaActivitySource.MessagingKafkaOffsetTag, result.Offset.Value);
            KafkaActivitySource.SetSuccess(activity);

            _logger.LogDebug(
                "Produced message {MessageId} to {Topic} partition {Partition} offset {Offset}",
                message.Id,
                result.Topic,
                result.Partition.Value,
                result.Offset.Value);

            return true;
        }
        catch (ProduceException<string, byte[]> ex)
        {
            KafkaActivitySource.RecordException(activity, ex);

            _logger.LogError(ex,
                "Failed to produce message {MessageId} to {Topic}: {Error}",
                message.Id, topic, ex.Error.Reason);
            return false;
        }
    }

    /// <inheritdoc />
    public void Produce<TMessage>(
        string topic,
        TMessage message,
        Action<DeliveryReport<string, byte[]>> deliveryHandler,
        string key = null,
        PublishMetadata metadata = null) where TMessage : Message
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic is required", nameof(topic));
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (deliveryHandler == null)
            throw new ArgumentNullException(nameof(deliveryHandler));

        var kafkaMessage = CreateKafkaMessage(message, key, metadata);

        _producer.Produce(topic, kafkaMessage, report =>
        {
            if (report.Error.IsError)
            {
                _logger.LogError(
                    "Delivery failed for message {MessageId} to {Topic}: {Error}",
                    message.Id, topic, report.Error.Reason);
            }
            else
            {
                _logger.LogDebug(
                    "Delivered message {MessageId} to {Topic} partition {Partition} offset {Offset}",
                    message.Id,
                    report.Topic,
                    report.Partition.Value,
                    report.Offset.Value);
            }

            deliveryHandler(report);
        });
    }

    /// <inheritdoc />
    public int Flush(TimeSpan timeout)
    {
        ThrowIfDisposed();
        return _producer.Flush(timeout);
    }

    private Message<string, byte[]> CreateKafkaMessage<TMessage>(
        TMessage message,
        string key,
        PublishMetadata metadata) where TMessage : Message
    {
        byte[] value;
        var headers = new Headers();

        if (_configuration.EnableCloudEvents)
        {
            // Serialize as CloudEvent
            var cloudEvent = _converter.ToCloudEvent(message, metadata);
            value = _converter.Serialize(cloudEvent);
            headers.Add("content-type", System.Text.Encoding.UTF8.GetBytes("application/cloudevents+json"));
        }
        else
        {
            // Standard JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(message, message.GetType());
            value = System.Text.Encoding.UTF8.GetBytes(json);
            headers.Add("content-type", System.Text.Encoding.UTF8.GetBytes("application/json"));
            headers.Add("message-type", System.Text.Encoding.UTF8.GetBytes(message.GetType().FullName));
        }

        return new Message<string, byte[]>
        {
            Key = key ?? message.Id.ToString(),
            Value = value,
            Headers = headers,
            Timestamp = new Timestamp(message.TimeStamp)
        };
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(typeof(KafkaProducer<TProducerType>).FullName);
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
            _logger.LogWarning(ex, "Error flushing producer during dispose");
        }

        _producer?.Dispose();
        _disposed = true;
    }
}

