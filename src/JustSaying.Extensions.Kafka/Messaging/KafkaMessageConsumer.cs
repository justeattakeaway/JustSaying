using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using CloudNative.CloudEvents;
using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Attributes;
using JustSaying.Extensions.Kafka.CloudEvents;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Handlers;
using JustSaying.Extensions.Kafka.Monitoring;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Messaging;

/// <summary>
/// Kafka message consumer that supports CloudEvents format while maintaining
/// compatibility with JustSaying Message types. Includes retry and DLT support.
/// </summary>
[IgnoreKafkaInWarmUp]
public class KafkaMessageConsumer : IDisposable
{
    private readonly IConsumer<string, byte[]> _consumer;
    private readonly KafkaConfiguration _configuration;
    private readonly CloudEventsMessageConverter _cloudEventsConverter;
    private readonly IMessageBodySerializationFactory _serializationFactory;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IKafkaConsumerMonitor _monitor;
    private readonly IKafkaMessageContextAccessor _contextAccessor;
    private readonly string _topic;
    private readonly uint _delayInMs;
    private bool _disposed;
    private CancellationTokenSource _cancellationTokenSource;

    // Retry handlers (lazily initialized per message type)
    private object _retryHandler;
    private object _topicChainingHandler;

    // Track delayed/paused tasks per partition for cancellation on rebalance
    private readonly ConcurrentDictionary<TopicPartition, CancellationTokenSource> _delayedTasks = new();
    private readonly ConcurrentDictionary<TopicPartition, bool> _pausedPartitions = new();

    public KafkaMessageConsumer(
        string topic,
        KafkaConfiguration configuration,
        IMessageBodySerializationFactory serializationFactory,
        ILoggerFactory loggerFactory,
        IKafkaConsumerMonitor monitor = null,
        IKafkaMessageContextAccessor contextAccessor = null)
    {
        _topic = topic ?? throw new ArgumentNullException(nameof(topic));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _serializationFactory = serializationFactory ?? throw new ArgumentNullException(nameof(serializationFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _monitor = monitor ?? NullKafkaConsumerMonitor.Instance;
        _contextAccessor = contextAccessor;

        _logger = loggerFactory.CreateLogger("JustSaying.Consume.Kafka");
        _delayInMs = configuration.DelayInMs;

        _cloudEventsConverter = new CloudEventsMessageConverter(serializationFactory, configuration.CloudEventsSource);

        var consumerConfig = configuration.GetConsumerConfig();
        _consumer = new ConsumerBuilder<string, byte[]>(consumerConfig)
            .SetErrorHandler((_, error) =>
            {
                if (error.IsFatal)
                    _logger.LogError("Fatal Kafka consumer error: {Code} - {Reason}", error.Code, error.Reason);
                else
                    _logger.LogWarning("Kafka consumer error: {Code} - {Reason}", error.Code, error.Reason);
            })
            .SetPartitionsAssignedHandler((_, partitions) => OnPartitionsAssigned(partitions))
            .SetPartitionsRevokedHandler((_, partitions) => OnPartitionsRevoked(partitions))
            .SetPartitionsLostHandler((_, partitions) => OnPartitionsLost(partitions))
            .Build();

        _consumer.Subscribe(_topic);
        
        _logger.LogInformation(
            "Subscribed to Kafka topic '{Topic}' with retry mode: {RetryMode}, DLT: {DltTopic}",
            _topic,
            configuration.Retry.Mode,
            configuration.DeadLetterTopic ?? "(none)");
    }

    #region Partition Rebalance Handling

    /// <summary>
    /// Called when partitions are assigned to this consumer.
    /// </summary>
    private void OnPartitionsAssigned(List<TopicPartition> partitions)
    {
        _logger.LogInformation(
            "Partitions assigned: [{Partitions}]",
            string.Join(", ", partitions.Select(p => $"{p.Topic}-{p.Partition.Value}")));

        // Clear any stale tracking for newly assigned partitions
        foreach (var partition in partitions)
        {
            _delayedTasks.TryRemove(partition, out _);
            _pausedPartitions.TryRemove(partition, out _);
        }
    }

    /// <summary>
    /// Called when partitions are being revoked (cooperative rebalance).
    /// We have a chance to commit offsets and clean up before partitions are reassigned.
    /// </summary>
    private void OnPartitionsRevoked(List<TopicPartitionOffset> partitions)
    {
        _logger.LogInformation(
            "Partitions revoked: [{Partitions}]",
            string.Join(", ", partitions.Select(p => $"{p.Topic}-{p.Partition.Value}")));

        CleanupPartitions(partitions.Select(p => p.TopicPartition));
    }

    /// <summary>
    /// Called when partitions are lost (non-cooperative, e.g., session timeout).
    /// The partitions have already been reassigned - just clean up local state.
    /// </summary>
    private void OnPartitionsLost(List<TopicPartitionOffset> partitions)
    {
        _logger.LogWarning(
            "Partitions lost (session timeout or error): [{Partitions}]",
            string.Join(", ", partitions.Select(p => $"{p.Topic}-{p.Partition.Value}")));

        CleanupPartitions(partitions.Select(p => p.TopicPartition));
    }

    /// <summary>
    /// Cleans up tracking state and cancels delayed tasks for the given partitions.
    /// </summary>
    private void CleanupPartitions(IEnumerable<TopicPartition> partitions)
    {
        foreach (var partition in partitions)
        {
            // Cancel any pending delayed tasks for this partition
            if (_delayedTasks.TryRemove(partition, out var cts))
            {
                _logger.LogDebug("Cancelling delayed task for partition {Partition}", partition);
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cancelling delayed task for partition {Partition}", partition);
                }
            }

            // Resume partition if it was paused (to clean up state)
            if (_pausedPartitions.TryRemove(partition, out _))
            {
                try
                {
                    _consumer.Resume(new[] { partition });
                    _logger.LogDebug("Resumed partition {Partition} during cleanup", partition);
                }
                catch (Exception ex)
                {
                    // Ignore - partition may no longer be assigned
                    _logger.LogDebug(ex, "Could not resume partition {Partition} (may no longer be assigned)", partition);
                }
            }
        }
    }

    /// <summary>
    /// Pauses a partition and tracks it for cleanup on rebalance.
    /// </summary>
    private void PausePartition(TopicPartition partition)
    {
        _consumer.Pause(new[] { partition });
        _pausedPartitions[partition] = true;
    }

    /// <summary>
    /// Resumes a partition and removes it from tracking.
    /// </summary>
    private void ResumePartition(TopicPartition partition)
    {
        try
        {
            _consumer.Resume(new[] { partition });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resuming partition {Partition}", partition);
        }
        finally
        {
            _pausedPartitions.TryRemove(partition, out _);
        }
    }

    /// <summary>
    /// Creates a cancellation token linked to both the main token and partition-specific cancellation.
    /// </summary>
    private CancellationTokenSource CreatePartitionLinkedCts(
        TopicPartition partition,
        CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _delayedTasks[partition] = cts;
        return cts;
    }

    /// <summary>
    /// Removes the partition-specific cancellation token source.
    /// </summary>
    private void RemovePartitionCts(TopicPartition partition)
    {
        if (_delayedTasks.TryRemove(partition, out var cts))
        {
            cts.Dispose();
        }
    }

    #endregion

    #region Message Context

    /// <summary>
    /// Sets the message context for the current async context.
    /// </summary>
    private void SetMessageContext(ConsumeResult<string, byte[]> consumeResult, int retryAttempt)
    {
        if (_contextAccessor == null)
            return;

        var headers = new Dictionary<string, string>();
        if (consumeResult.Message.Headers != null)
        {
            foreach (var header in consumeResult.Message.Headers)
            {
                try
                {
                    headers[header.Key] = Encoding.UTF8.GetString(header.GetValueBytes());
                }
                catch
                {
                    // Skip headers that can't be decoded as UTF-8
                }
            }
        }

        _contextAccessor.Context = new KafkaMessageContext
        {
            Topic = consumeResult.Topic,
            Partition = consumeResult.Partition.Value,
            Offset = consumeResult.Offset.Value,
            Key = consumeResult.Message.Key,
            Timestamp = consumeResult.Message.Timestamp.UtcDateTime,
            Headers = headers,
            ReceivedAt = DateTime.UtcNow,
            GroupId = _configuration.GroupId,
            ConsumerId = null, // Auto-generated
            RetryAttempt = retryAttempt,
            CloudEventType = headers.TryGetValue("ce_type", out var ceType) ? ceType : null,
            CloudEventSource = headers.TryGetValue("ce_source", out var ceSource) ? ceSource : null,
            CloudEventId = headers.TryGetValue("ce_id", out var ceId) ? ceId : null
        };
    }

    /// <summary>
    /// Updates the retry attempt in the current context.
    /// </summary>
    private void UpdateContextRetryAttempt(int retryAttempt)
    {
        if (_contextAccessor?.Context != null)
        {
            _contextAccessor.Context.RetryAttempt = retryAttempt;
        }
    }

    /// <summary>
    /// Clears the message context after processing.
    /// </summary>
    private void ClearMessageContext()
    {
        if (_contextAccessor != null)
        {
            _contextAccessor.Context = null;
        }
    }

    #endregion

    /// <summary>
    /// Gets or creates the in-process retry handler for the specified message type.
    /// </summary>
    private InProcessRetryHandler<T> GetOrCreateInProcessRetryHandler<T>() where T : Message
    {
        if (_retryHandler is InProcessRetryHandler<T> existing)
            return existing;

        var handler = new InProcessRetryHandler<T>(
            _configuration.Retry,
            _configuration.DeadLetterTopic,
            _configuration,
            _loggerFactory);

        _retryHandler = handler;
        return handler;
    }

    /// <summary>
    /// Gets or creates the topic chaining failure handler for the specified message type.
    /// </summary>
    private KafkaFailureHandler<T> GetOrCreateTopicChainingHandler<T>() where T : Message
    {
        if (_topicChainingHandler is KafkaFailureHandler<T> existing)
            return existing;

        var targetTopic = _configuration.FailureTopic ?? _configuration.DeadLetterTopic;
        if (string.IsNullOrWhiteSpace(targetTopic))
            return null;

        var handler = new KafkaFailureHandler<T>(
            targetTopic,
            _configuration,
            _loggerFactory);

        _topicChainingHandler = handler;
        return handler;
    }

    /// <summary>
    /// Handles the processing delay for retry topics in topic chaining mode.
    /// </summary>
    private async Task HandleDelayAsync(ConsumeResult<string, byte[]> consumeResult, CancellationToken cancellationToken)
    {
        if (_delayInMs == 0)
            return;

        var expectedTime = consumeResult.Message.Timestamp.UtcDateTime.AddMilliseconds(_delayInMs);
        var delayMs = (int)(expectedTime - DateTime.UtcNow).TotalMilliseconds;

        if (delayMs > 0)
        {
            _logger.LogDebug(
                "Delaying message processing for {DelayMs}ms (retry topic delay)",
                delayMs);

            var partition = consumeResult.TopicPartition;
            var linkedCts = CreatePartitionLinkedCts(partition, cancellationToken);

            PausePartition(partition);

            try
            {
                await Task.Delay(delayMs, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Partition rebalance occurred - log and rethrow to skip this message
                _logger.LogDebug("Delay cancelled due to partition rebalance for partition {Partition}", partition);
                throw;
            }
            finally
            {
                RemovePartitionCts(partition);
                ResumePartition(partition);
            }
        }
    }

    /// <summary>
    /// Processes a message with retry support (for StartAsync method).
    /// </summary>
    private async Task<bool> ProcessMessageWithRetryAsync<T>(
        ConsumeResult<string, byte[]> consumeResult,
        T message,
        IHandlerAsync<T> handler,
        CancellationToken cancellationToken) where T : Message
    {
        var (handled, _) = await ProcessMessageWithRetryAndMonitoringAsync(consumeResult, message, handler, cancellationToken);
        return handled;
    }

    /// <summary>
    /// Processes a message with retry support and returns retry attempt count for monitoring.
    /// </summary>
    private async Task<(bool handled, int retryAttempt)> ProcessMessageWithRetryAndMonitoringAsync<T>(
        ConsumeResult<string, byte[]> consumeResult,
        T message,
        IHandlerAsync<T> handler,
        CancellationToken cancellationToken) where T : Message
    {
        // Set the message context if accessor is available
        SetMessageContext(consumeResult, 0);

        try
        {
            // Handle delay for topic chaining retry topics
            await HandleDelayAsync(consumeResult, cancellationToken);

            var retryMode = _configuration.Retry.Mode;

            if (retryMode == RetryMode.InProcess)
            {
                // In-process retry mode (default, cost-optimized) with monitoring
                return await ExecuteWithInProcessRetryAndMonitoringAsync(consumeResult, message, handler, cancellationToken);
            }
            else
            {
                // Topic chaining mode - single attempt, forward to failure topic on error
                var success = await ExecuteWithTopicChainingAndMonitoringAsync(consumeResult, message, handler, cancellationToken);
                return (success, 1);
            }
        }
        finally
        {
            // Clear the context after processing
            ClearMessageContext();
        }
    }

    /// <summary>
    /// Executes handler with in-process retry and monitoring.
    /// </summary>
    private async Task<(bool handled, int retryAttempt)> ExecuteWithInProcessRetryAndMonitoringAsync<T>(
        ConsumeResult<string, byte[]> consumeResult,
        T message,
        IHandlerAsync<T> handler,
        CancellationToken cancellationToken) where T : Message
    {
        var maxAttempts = _configuration.Retry.MaxRetryAttempts;
        var attempt = 0;
        Exception lastException = null;

        while (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
        {
            attempt++;

            // Update context with current retry attempt
            UpdateContextRetryAttempt(attempt);

            try
            {
                var result = await handler.Handle(message).ConfigureAwait(false);

                if (result)
                {
                    return (true, attempt);
                }

                lastException = new InvalidOperationException(
                    $"Handler returned false for message {message.Id}");
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            var willRetry = attempt < maxAttempts;
            NotifyMessageFailed(consumeResult, message, lastException, attempt, willRetry);

            if (willRetry)
            {
                var backoff = CalculateBackoff(attempt);

                var partition = consumeResult.TopicPartition;

                _logger.LogDebug(
                    "Pausing partition {Partition} for {BackoffMs}ms before retry",
                    partition.Partition.Value, backoff.TotalMilliseconds);

                var linkedCts = CreatePartitionLinkedCts(partition, cancellationToken);
                PausePartition(partition);

                try
                {
                    await Task.Delay(backoff, linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (linkedCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    // Partition rebalance occurred - stop retrying and let message be reprocessed
                    _logger.LogDebug("Retry delay cancelled due to partition rebalance for partition {Partition}", partition);
                    throw;
                }
                finally
                {
                    RemovePartitionCts(partition);
                    ResumePartition(partition);
                }
            }
        }

        // All retries exhausted - forward to DLT
        if (lastException != null)
        {
            await ForwardToFailureTopicAsync(consumeResult, message, lastException, attempt, cancellationToken);
        }

        return (false, attempt);
    }

    /// <summary>
    /// Calculates the backoff duration for a retry attempt.
    /// </summary>
    private TimeSpan CalculateBackoff(int attempt)
    {
        var retryConfig = _configuration.Retry;

        if (!retryConfig.ExponentialBackoff)
            return retryConfig.InitialBackoff;

        var multiplier = Math.Pow(2, attempt - 1);
        var backoff = TimeSpan.FromTicks((long)(retryConfig.InitialBackoff.Ticks * multiplier));

        return backoff > retryConfig.MaxBackoff ? retryConfig.MaxBackoff : backoff;
    }

    /// <summary>
    /// Executes handler once with monitoring and forwards to failure topic on error (topic chaining mode).
    /// </summary>
    private async Task<bool> ExecuteWithTopicChainingAndMonitoringAsync<T>(
        ConsumeResult<string, byte[]> consumeResult,
        T message,
        IHandlerAsync<T> handler,
        CancellationToken cancellationToken) where T : Message
    {
        try
        {
            var result = await handler.Handle(message).ConfigureAwait(false);
            if (!result)
            {
                var exception = new InvalidOperationException($"Handler returned false for message {message.Id}");
                NotifyMessageFailed(consumeResult, message, exception, 1, willRetry: false);
                await ForwardToFailureTopicAsync(consumeResult, message, exception, 1, cancellationToken);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler failed for message {MessageId}", message.Id);
            NotifyMessageFailed(consumeResult, message, ex, 1, willRetry: false);
            await ForwardToFailureTopicAsync(consumeResult, message, ex, 1, cancellationToken);
            return false;
        }
    }


    /// <summary>
    /// Forwards a failed message to the configured failure/DLT topic.
    /// </summary>
    private async Task ForwardToFailureTopicAsync<T>(
        ConsumeResult<string, byte[]> consumeResult,
        T message,
        Exception exception,
        int totalAttempts,
        CancellationToken cancellationToken) where T : Message
    {
        var failureHandler = GetOrCreateTopicChainingHandler<T>();
        var targetTopic = _configuration.FailureTopic ?? _configuration.DeadLetterTopic;

        if (failureHandler == null)
        {
            _logger.LogWarning(
                "No failure topic configured. Message {MessageId} will be committed and lost.",
                message.Id);
            return;
        }

        var context = new Handlers.MessageFailureContext<T>
        {
            KafkaResult = consumeResult,
            Message = message,
            Topic = consumeResult.Topic,
            Partition = consumeResult.Partition.Value,
            Offset = consumeResult.Offset.Value,
            RetryAttempt = totalAttempts,
            RetriesExhausted = true
        };

        try
        {
            await failureHandler.OnFailureAsync(context, exception, cancellationToken);

            // Notify monitor of dead-lettering
            _monitor.OnMessageDeadLettered(new MessageDeadLetteredContext<T>
            {
                Topic = consumeResult.Topic,
                DeadLetterTopic = targetTopic,
                Partition = consumeResult.Partition.Value,
                Offset = consumeResult.Offset.Value,
                Message = message,
                Exception = exception,
                TotalAttempts = totalAttempts
            });
        }
        catch (Exception forwardEx)
        {
            _logger.LogCritical(forwardEx,
                "Failed to forward message {MessageId} to failure topic. Message may be lost!",
                message.Id);
        }
    }

    /// <summary>
    /// Notifies the monitor that a message was received.
    /// </summary>
    private void NotifyMessageReceived<T>(ConsumeResult<string, byte[]> consumeResult, T message) where T : Message
    {
        _monitor.OnMessageReceived(new MessageReceivedContext<T>
        {
            Topic = consumeResult.Topic,
            Partition = consumeResult.Partition.Value,
            Offset = consumeResult.Offset.Value,
            MessageTimestamp = consumeResult.Message.Timestamp.UtcDateTime,
            ReceivedAt = DateTime.UtcNow,
            Message = message
        });
    }

    /// <summary>
    /// Notifies the monitor that a message was successfully processed.
    /// </summary>
    private void NotifyMessageProcessed<T>(
        ConsumeResult<string, byte[]> consumeResult,
        T message,
        TimeSpan duration,
        int retryAttempt = 1) where T : Message
    {
        _monitor.OnMessageProcessed(new MessageProcessedContext<T>
        {
            Topic = consumeResult.Topic,
            Partition = consumeResult.Partition.Value,
            Offset = consumeResult.Offset.Value,
            Message = message,
            ProcessingDuration = duration,
            RetryAttempt = retryAttempt
        });
    }

    /// <summary>
    /// Notifies the monitor that a message processing failed.
    /// </summary>
    private void NotifyMessageFailed<T>(
        ConsumeResult<string, byte[]> consumeResult,
        T message,
        Exception exception,
        int retryAttempt,
        bool willRetry) where T : Message
    {
        _monitor.OnMessageFailed(new Monitoring.MessageFailedContext<T>
        {
            Topic = consumeResult.Topic,
            Partition = consumeResult.Partition.Value,
            Offset = consumeResult.Offset.Value,
            Message = message,
            Exception = exception,
            RetryAttempt = retryAttempt,
            WillRetry = willRetry
        });
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

                    // Handle delay for retry topics
                    await HandleDelayAsync(consumeResult, _cancellationTokenSource.Token);

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

                        bool handled;
                        
                        if (_configuration.Retry.Mode == RetryMode.InProcess && _configuration.Retry.MaxRetryAttempts > 0)
                        {
                            // Use in-process retry for middleware
                            var retryHandler = GetOrCreateInProcessRetryHandler<T>();
                            handled = await retryHandler.ExecuteWithRetryAsync(
                                consumeResult,
                                message,
                                async _ => await middleware.RunAsync(context, null, _cancellationTokenSource.Token).ConfigureAwait(false),
                                _consumer,
                                _cancellationTokenSource.Token);
                        }
                        else
                        {
                            // Execute middleware without retry (topic chaining or no retry)
                            try
                            {
                                handled = await middleware.RunAsync(context, null, _cancellationTokenSource.Token).ConfigureAwait(false);
                                if (!handled)
                                {
                                    await ForwardToFailureTopicAsync(
                                        consumeResult, message,
                                        new InvalidOperationException("Middleware returned false"),
                                        1, _cancellationTokenSource.Token);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Middleware failed for message {MessageId}", message.Id);
                                await ForwardToFailureTopicAsync(consumeResult, message, ex, 1, _cancellationTokenSource.Token);
                                handled = false;
                            }
                        }

                        // Always commit after processing (success or DLT forwarding)
                        _consumer.Commit(consumeResult);

                        if (handled)
                        {
                            _logger.LogInformation(
                                "Successfully processed and committed message {MessageId} from topic '{Topic}'",
                                message.Id,
                                _topic);
                        }
                    }
                    else
                    {
                        // Deserialization failed - commit to skip message
                        _consumer.Commit(consumeResult);
                        _logger.LogWarning(
                            "Deserialization failed for message from topic '{Topic}' partition {Partition} offset {Offset}. Message committed and skipped.",
                            _topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
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

                    // Handle delay for retry topics
                    await HandleDelayAsync(consumeResult, _cancellationTokenSource.Token);

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

                        bool handled;

                        if (_configuration.Retry.Mode == RetryMode.InProcess && _configuration.Retry.MaxRetryAttempts > 0)
                        {
                            // Use in-process retry for middleware
                            var retryHandler = GetOrCreateInProcessRetryHandler<T>();
                            handled = await retryHandler.ExecuteWithRetryAsync(
                                consumeResult,
                                message,
                                async _ => await middleware.RunAsync(context, null, _cancellationTokenSource.Token).ConfigureAwait(false),
                                _consumer,
                                _cancellationTokenSource.Token);
                        }
                        else
                        {
                            // Execute middleware without retry
                            try
                            {
                                handled = await middleware.RunAsync(context, null, _cancellationTokenSource.Token).ConfigureAwait(false);
                                if (!handled)
                                {
                                    await ForwardToFailureTopicAsync(
                                        consumeResult, message,
                                        new InvalidOperationException("Middleware returned false"),
                                        1, _cancellationTokenSource.Token);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Middleware failed for message {MessageId}", message.Id);
                                await ForwardToFailureTopicAsync(consumeResult, message, ex, 1, _cancellationTokenSource.Token);
                                handled = false;
                            }
                        }

                        // Always commit after processing
                        _consumer.Commit(consumeResult);

                        if (handled)
                        {
                            _logger.LogInformation(
                                "Successfully processed and committed message {MessageId} from topic '{Topic}'",
                                message.Id,
                                _topic);
                        }
                    }
                    else
                    {
                        // Deserialization failed - commit to skip
                        _consumer.Commit(consumeResult);
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

                        // Notify monitor of message receipt
                        NotifyMessageReceived(consumeResult, message);

                        // Process with retry support and timing
                        var stopwatch = Stopwatch.StartNew();
                        var (handled, retryAttempt) = await ProcessMessageWithRetryAndMonitoringAsync(
                            consumeResult, message, handler, _cancellationTokenSource.Token);
                        stopwatch.Stop();

                        // Always commit after processing (success or DLT forwarding)
                        _consumer.Commit(consumeResult);

                        if (handled)
                        {
                            NotifyMessageProcessed(consumeResult, message, stopwatch.Elapsed, retryAttempt);
                            _logger.LogInformation(
                                "Successfully processed and committed message {MessageId} from topic '{Topic}'",
                                message.Id,
                                _topic);
                        }
                    }
                    else
                    {
                        // Deserialization failed - commit to skip
                        _consumer.Commit(consumeResult);
                        _logger.LogWarning(
                            "Deserialization failed for message from topic '{Topic}' partition {Partition} offset {Offset}. Message committed and skipped.",
                            _topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
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
            var contentTypeHeader = kafkaMessage.Headers?.FirstOrDefault(h => h.Key == "content-type");
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

        // Dispose retry handlers
        (_retryHandler as IDisposable)?.Dispose();
        (_topicChainingHandler as IDisposable)?.Dispose();

        _disposed = true;
    }
}
