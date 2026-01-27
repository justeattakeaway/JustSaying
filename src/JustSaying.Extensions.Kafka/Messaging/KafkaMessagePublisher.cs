using System.Diagnostics;
using System.Net;
using System.Text;
using CloudNative.CloudEvents;
using Confluent.Kafka;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Extensions.Kafka.CloudEvents;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Partitioning;
using JustSaying.Extensions.Kafka.Tracing;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Messaging;

/// <summary>
/// Kafka message publisher that supports CloudEvents format while maintaining
/// compatibility with JustSaying Message types.
/// </summary>
public class KafkaMessagePublisher : IMessagePublisher, IMessageBatchPublisher, IDisposable
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly KafkaConfiguration _configuration;
    private readonly CloudEventsMessageConverter _cloudEventsConverter;
    private readonly IMessageBodySerializationFactory _serializationFactory;
    private readonly ILogger _logger;
    private readonly string _topic;
    private bool _disposed;

    public Action<JustSaying.AwsTools.MessageHandling.MessageResponse, Message> MessageResponseLogger { get; set; }
    public Action<JustSaying.AwsTools.MessageHandling.MessageBatchResponse, IReadOnlyCollection<Message>> MessageBatchResponseLogger { get; set; }

    public KafkaMessagePublisher(
        string topic,
        KafkaConfiguration configuration,
        IMessageBodySerializationFactory serializationFactory,
        ILoggerFactory loggerFactory)
    {
        _topic = topic ?? throw new ArgumentNullException(nameof(topic));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _serializationFactory = serializationFactory ?? throw new ArgumentNullException(nameof(serializationFactory));
        
        if (loggerFactory == null)
            throw new ArgumentNullException(nameof(loggerFactory));

        _logger = loggerFactory.CreateLogger("JustSaying.Publish.Kafka");
        
        _cloudEventsConverter = new CloudEventsMessageConverter(serializationFactory, configuration.CloudEventsSource);
        
        var producerConfig = configuration.GetProducerConfig();
        _producer = new ProducerBuilder<string, byte[]>(producerConfig)
            .SetErrorHandler((_, error) => _logger.LogError("Kafka producer error: {Reason}", error.Reason))
            .Build();
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task PublishAsync(Message message, CancellationToken cancellationToken)
        => PublishAsync(message, null, cancellationToken);

    /// <inheritdoc/>
    public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        ThrowIfDisposed();

        // Use partition key strategy if configured, otherwise use UniqueKey
        var messageKey = _configuration.PartitionKeyStrategy != null
            ? _configuration.PartitionKeyStrategy.GetPartitionKey(message, _topic)
            : message.UniqueKey();

        // Start produce activity for distributed tracing
        using var activity = KafkaActivitySource.StartProduceActivity(
            _topic,
            message.Id.ToString(),
            messageKey);

        try
        {
            var kafkaMessage = CreateKafkaMessage(message, metadata);

            // Inject trace context into headers
            if (activity != null)
            {
                TraceContextPropagator.InjectTraceContext(kafkaMessage.Headers, activity);
            }
            
            var deliveryResult = await _producer.ProduceAsync(_topic, kafkaMessage, cancellationToken)
                .ConfigureAwait(false);

            activity?.SetTag(KafkaActivitySource.MessagingKafkaPartitionTag, deliveryResult.Partition.Value);
            activity?.SetTag(KafkaActivitySource.MessagingKafkaOffsetTag, deliveryResult.Offset.Value);
            KafkaActivitySource.SetSuccess(activity);

            _logger.LogInformation(
                "Published message {MessageId} of type {MessageType} to Kafka topic '{Topic}' at partition {Partition}, offset {Offset}",
                message.Id,
                message.GetType().FullName,
                _topic,
                deliveryResult.Partition.Value,
                deliveryResult.Offset.Value);

            if (MessageResponseLogger != null)
            {
                var responseData = new MessageResponse
                {
                    MessageId = deliveryResult.Message.Key,
                    HttpStatusCode = System.Net.HttpStatusCode.OK
                };
                MessageResponseLogger.Invoke(responseData, message);
            }
        }
        catch (ProduceException<string, byte[]> ex)
        {
            KafkaActivitySource.RecordException(activity, ex);

            _logger.LogError(ex, "Failed to publish message {MessageId} to Kafka topic '{Topic}'", message.Id, _topic);
            throw new PublishException($"Failed to publish message to Kafka topic '{_topic}'.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
    {
        if (messages == null)
            throw new ArgumentNullException(nameof(messages));

        ThrowIfDisposed();

        var messageList = messages.ToList();
        var tasks = new List<Task<DeliveryResult<string, byte[]>>>();
        var successfulIds = new List<string>();
        var failedIds = new List<string>();

        try
        {
            foreach (var message in messageList)
            {
                var kafkaMessage = CreateKafkaMessage(message, metadata);
                tasks.Add(_producer.ProduceAsync(_topic, kafkaMessage, cancellationToken));
            }

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var result in results)
            {
                successfulIds.Add(result.Message.Key);
            }

            _logger.LogInformation(
                "Published batch of {MessageCount} messages to Kafka topic '{Topic}'",
                successfulIds.Count,
                _topic);

            if (MessageBatchResponseLogger != null)
            {
                var responseData = new MessageBatchResponse
                {
                    SuccessfulMessageIds = successfulIds.ToArray(),
                    FailedMessageIds = failedIds.ToArray(),
                    HttpStatusCode = System.Net.HttpStatusCode.OK
                };
                MessageBatchResponseLogger.Invoke(responseData, messageList);
            }
        }
        catch (ProduceException<string, byte[]> ex)
        {
            _logger.LogError(ex, "Failed to publish batch of messages to Kafka topic '{Topic}'", _topic);
            throw new PublishBatchException($"Failed to publish batch of messages to Kafka topic '{_topic}'.", ex);
        }
    }

    /// <inheritdoc/>
    public InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {
            Topic = _topic,
            BootstrapServers = _configuration.BootstrapServers,
            CloudEventsEnabled = _configuration.EnableCloudEvents
        });
    }

    private Confluent.Kafka.Message<string, byte[]> CreateKafkaMessage(Message message, PublishMetadata metadata)
    {
        // Use partition key strategy if configured, otherwise use UniqueKey
        var key = _configuration.PartitionKeyStrategy != null
            ? _configuration.PartitionKeyStrategy.GetPartitionKey(message, _topic)
            : message.UniqueKey();

        byte[] value;
        var headers = new Headers();

        if (_configuration.EnableCloudEvents)
        {
            // Use CloudEvents format
            var cloudEvent = ToCloudEventDynamic(message, metadata);
            value = _cloudEventsConverter.Serialize(cloudEvent);
            
            // Add CloudEvents headers
            headers.Add("content-type", Encoding.UTF8.GetBytes("application/cloudevents+json"));
        }
        else
        {
            // Use standard serialization (backward compatibility)
            var serializer = GetSerializer(message.GetType());
            var messageBody = serializer.Serialize(message);
            value = Encoding.UTF8.GetBytes(messageBody);
            
            // Add standard headers
            headers.Add("message-type", Encoding.UTF8.GetBytes(message.GetType().FullName));
            headers.Add("content-type", Encoding.UTF8.GetBytes("application/json"));
        }

        // Add custom metadata headers
        if (metadata?.MessageAttributes != null)
        {
            foreach (var attr in metadata.MessageAttributes)
            {
                if (attr.Value?.StringValue != null)
                {
                    headers.Add($"x-attr-{attr.Key}", Encoding.UTF8.GetBytes(attr.Value.StringValue));
                }
            }
        }

        return new Confluent.Kafka.Message<string, byte[]>
        {
            Key = key,
            Value = value,
            Headers = headers,
            Timestamp = new Timestamp(message.TimeStamp)
        };
    }

    private IMessageBodySerializer GetSerializer(Type messageType)
    {
        var method = typeof(IMessageBodySerializationFactory)
            .GetMethod(nameof(IMessageBodySerializationFactory.GetSerializer))
            ?.MakeGenericMethod(messageType);

        return (IMessageBodySerializer)method?.Invoke(_serializationFactory, null);
    }

    private CloudNative.CloudEvents.CloudEvent ToCloudEventDynamic(Message message, PublishMetadata metadata)
    {
        var method = typeof(CloudEventsMessageConverter)
            .GetMethod(nameof(CloudEventsMessageConverter.ToCloudEvent))
            ?.MakeGenericMethod(message.GetType());

        return (CloudNative.CloudEvents.CloudEvent)method?.Invoke(_cloudEventsConverter, new object[] { message, metadata });
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
        _disposed = true;
    }
}
