using System.Text;
using CloudNative.CloudEvents;
using Confluent.Kafka;
using JustSaying.Extensions.Kafka.CloudEvents;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Messaging;

/// <summary>
/// Kafka message consumer that supports CloudEvents format while maintaining
/// compatibility with JustSaying Message types.
/// </summary>
public class KafkaMessageConsumer : IDisposable
{
    private readonly IConsumer<string, byte[]> _consumer;
    private readonly KafkaConfiguration _configuration;
    private readonly CloudEventsMessageConverter _cloudEventsConverter;
    private readonly IMessageBodySerializationFactory _serializationFactory;
    private readonly ILogger _logger;
    private readonly string _topic;
    private bool _disposed;
    private CancellationTokenSource _cancellationTokenSource;

    public KafkaMessageConsumer(
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

        _logger = loggerFactory.CreateLogger("JustSaying.Consume.Kafka");
        
        _cloudEventsConverter = new CloudEventsMessageConverter(serializationFactory, configuration.CloudEventsSource);
        
        var consumerConfig = configuration.GetConsumerConfig();
        _consumer = new ConsumerBuilder<string, byte[]>(consumerConfig)
            .SetErrorHandler((_, error) => _logger.LogError("Kafka consumer error: {Reason}", error.Reason))
            .Build();

        _consumer.Subscribe(_topic);
        _logger.LogInformation("Subscribed to Kafka topic '{Topic}'", _topic);
    }

    /// <summary>
    /// Starts consuming messages with middleware support.
    /// </summary>
    public async Task StartConsumingWithMiddleware<T>(IHandlerAsync<T> handler, MiddlewareBase<HandleMessageContext, bool> middleware, CancellationToken cancellationToken) where T : Message
    {
        ThrowIfDisposed();

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(_cancellationTokenSource.Token);
                    
                    if (consumeResult?.Message == null)
                        continue;

                    var message = DeserializeMessage<T>(consumeResult.Message);
                    
                    if (message != null)
                    {
                        _logger.LogDebug(
                            "Received message {MessageId} from topic '{Topic}' partition {Partition} offset {Offset}",
                            message.Id,
                            _topic,
                            consumeResult.Partition.Value,
                            consumeResult.Offset.Value);

                        // Create a dummy raw AWS message for middleware compatibility
                        var rawMessage = new Amazon.SQS.Model.Message
                        {
                            MessageId = message.Id.ToString(),
                            Body = string.Empty
                        };

                        // Create middleware context with Kafka-specific implementations
                        var context = new HandleMessageContext(
                            queueName: _topic,
                            rawMessage: rawMessage,
                            message: message,
                            messageType: typeof(T),
                            visibilityUpdater: new KafkaVisibilityUpdater(),
                            messageDeleter: new KafkaMessageDeleter(),
                            queueUri: new Uri($"kafka://{_topic}"),
                            messageAttributes: new MessageAttributes());

                        // Execute through middleware pipeline
                        var handled = await middleware.RunAsync(context, null, _cancellationTokenSource.Token).ConfigureAwait(false);

                        if (handled)
                        {
                            _consumer.Commit(consumeResult);
                            
                            _logger.LogInformation(
                                "Successfully processed and committed message {MessageId} from topic '{Topic}'",
                                message.Id,
                                _topic);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Handler returned false for message {MessageId} from topic '{Topic}'",
                                message.Id,
                                _topic);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming from Kafka topic '{Topic}'", _topic);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Kafka consumer for topic '{Topic}' was cancelled", _topic);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing message from topic '{Topic}'", _topic);
                }
            }
        }, _cancellationTokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts consuming messages with middleware support (non-async overload for backward compatibility).
    /// </summary>
    public void StartConsuming<T>(IHandlerAsync<T> handler, MiddlewareBase<HandleMessageContext, bool> middleware) where T : Message
    {
        ThrowIfDisposed();

        _cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(_cancellationTokenSource.Token);
                    
                    if (consumeResult?.Message == null)
                        continue;

                    var message = DeserializeMessage<T>(consumeResult.Message);
                    
                    if (message != null)
                    {
                        _logger.LogDebug(
                            "Received message {MessageId} from topic '{Topic}' partition {Partition} offset {Offset}",
                            message.Id,
                            _topic,
                            consumeResult.Partition.Value,
                            consumeResult.Offset.Value);

                        // Create a dummy raw AWS message for middleware compatibility
                        var rawMessage = new Amazon.SQS.Model.Message
                        {
                            MessageId = message.Id.ToString(),
                            Body = string.Empty
                        };

                        // Create middleware context with Kafka-specific implementations
                        var context = new HandleMessageContext(
                            queueName: _topic,
                            rawMessage: rawMessage,
                            message: message,
                            messageType: typeof(T),
                            visibilityUpdater: new KafkaVisibilityUpdater(),
                            messageDeleter: new KafkaMessageDeleter(),
                            queueUri: new Uri($"kafka://{_topic}"),
                            messageAttributes: new MessageAttributes());

                        // Execute through middleware pipeline
                        var handled = await middleware.RunAsync(context, null, _cancellationTokenSource.Token).ConfigureAwait(false);

                        if (handled)
                        {
                            _consumer.Commit(consumeResult);
                            
                            _logger.LogInformation(
                                "Successfully processed and committed message {MessageId} from topic '{Topic}'",
                                message.Id,
                                _topic);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Handler returned false for message {MessageId} from topic '{Topic}'",
                                message.Id,
                                _topic);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming from Kafka topic '{Topic}'", _topic);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Kafka consumer for topic '{Topic}' was cancelled", _topic);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing message from topic '{Topic}'", _topic);
                }
            }
        }, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Starts consuming messages and processing them with the provided handler.
    /// </summary>
    public async Task StartAsync<T>(
        IHandlerAsync<T> handler,
        CancellationToken cancellationToken = default) where T : Message
    {
        ThrowIfDisposed();

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(_cancellationTokenSource.Token);
                    
                    if (consumeResult?.Message == null)
                        continue;

                    var message = DeserializeMessage<T>(consumeResult.Message);
                    
                    if (message != null)
                    {
                        _logger.LogDebug(
                            "Received message {MessageId} from topic '{Topic}' partition {Partition} offset {Offset}",
                            message.Id,
                            _topic,
                            consumeResult.Partition.Value,
                            consumeResult.Offset.Value);

                        var handled = await handler.Handle(message).ConfigureAwait(false);

                        if (handled)
                        {
                            _consumer.Commit(consumeResult);
                            
                            _logger.LogInformation(
                                "Successfully processed and committed message {MessageId} from topic '{Topic}'",
                                message.Id,
                                _topic);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Handler returned false for message {MessageId} from topic '{Topic}'",
                                message.Id,
                                _topic);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming from Kafka topic '{Topic}'", _topic);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Kafka consumer for topic '{Topic}' was cancelled", _topic);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing message from topic '{Topic}'", _topic);
                }
            }
        }, _cancellationTokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops consuming messages.
    /// </summary>
    public Task StopAsync()
    {
        _cancellationTokenSource?.Cancel();
        return Task.CompletedTask;
    }

    private T DeserializeMessage<T>(Confluent.Kafka.Message<string, byte[]> kafkaMessage) where T : Message
    {
        try
        {
            // Check if this is a CloudEvents message
            var contentTypeHeader = kafkaMessage.Headers.FirstOrDefault(h => h.Key == "content-type");
            bool isCloudEvent = false;

            if (contentTypeHeader != null)
            {
                var contentType = Encoding.UTF8.GetString(contentTypeHeader.GetValueBytes());
                isCloudEvent = contentType.Contains("cloudevents");
            }

            Message message;

            if (isCloudEvent && _configuration.EnableCloudEvents)
            {
                // Deserialize as CloudEvent
                var cloudEvent = _cloudEventsConverter.Deserialize(kafkaMessage.Value);
                message = _cloudEventsConverter.FromCloudEvent<T>(cloudEvent);
            }
            else
            {
                // Standard deserialization (backward compatibility)
                var messageBody = Encoding.UTF8.GetString(kafkaMessage.Value);
                var serializer = _serializationFactory.GetSerializer<T>();
                message = serializer.Deserialize(messageBody);
            }

            // Restore Kafka metadata if needed
            if (message != null && kafkaMessage.Timestamp.Type != TimestampType.NotAvailable)
            {
                // Keep the original message timestamp if it was set properly
                // Otherwise, you could use: message.TimeStamp = kafkaMessage.Timestamp.UtcDateTime;
            }

            return message as T;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize message from Kafka");
            return null;
        }
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

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _consumer?.Close();
        _consumer?.Dispose();
        _disposed = true;
    }
}
