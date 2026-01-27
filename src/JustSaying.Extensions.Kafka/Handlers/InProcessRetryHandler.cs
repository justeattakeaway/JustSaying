using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Handlers;

/// <summary>
/// Handles retries in-process with partition pause, then forwards to DLT.
/// This is the cost-optimized default mode.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
internal class InProcessRetryHandler<T> : IDisposable where T : Message
{
    private static readonly Random RandomInstance = new Random();
    private static readonly object RandomLock = new object();
    
    private readonly RetryConfiguration _retryConfig;
    private readonly KafkaFailureHandler<T> _dltHandler;
    private readonly ILogger _logger;
    private bool _disposed;

    public InProcessRetryHandler(
        RetryConfiguration retryConfig,
        string deadLetterTopic,
        KafkaConfiguration kafkaConfig,
        ILoggerFactory loggerFactory)
    {
        _retryConfig = retryConfig ?? new RetryConfiguration();
        _logger = loggerFactory.CreateLogger("JustSaying.Kafka.InProcessRetry");

        if (!string.IsNullOrWhiteSpace(deadLetterTopic))
        {
            _dltHandler = new KafkaFailureHandler<T>(
                deadLetterTopic, kafkaConfig, loggerFactory);
            
            _logger.LogInformation(
                "In-process retry handler initialized with DLT '{DeadLetterTopic}'. " +
                "MaxAttempts={MaxAttempts}, InitialBackoff={InitialBackoff}ms, ExponentialBackoff={ExponentialBackoff}",
                deadLetterTopic,
                _retryConfig.MaxRetryAttempts,
                _retryConfig.InitialBackoff.TotalMilliseconds,
                _retryConfig.ExponentialBackoff);
        }
        else
        {
            _logger.LogWarning(
                "In-process retry handler initialized WITHOUT DLT. " +
                "Failed messages will be logged and committed (lost).");
        }
    }

    /// <summary>
    /// Executes message handling with in-process retry.
    /// </summary>
    /// <param name="consumeResult">The Kafka consume result.</param>
    /// <param name="message">The deserialized message.</param>
    /// <param name="handler">The handler function to execute.</param>
    /// <param name="consumer">The Kafka consumer (for partition pause/resume).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if handling succeeded, false if all retries exhausted.</returns>
    public async Task<bool> ExecuteWithRetryAsync(
        ConsumeResult<string, byte[]> consumeResult,
        T message,
        Func<T, Task<bool>> handler,
        IConsumer<string, byte[]> consumer,
        CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InProcessRetryHandler<T>));

        var attempt = 0;
        var maxAttempts = Math.Max(1, _retryConfig.MaxRetryAttempts); // At least 1 attempt
        Exception lastException = null;

        while (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
        {
            attempt++;

            try
            {
                var result = await handler(message).ConfigureAwait(false);

                if (result)
                {
                    if (attempt > 1)
                    {
                        _logger.LogInformation(
                            "Message {MessageId} succeeded on attempt {Attempt}/{MaxAttempts}",
                            message.Id, attempt, maxAttempts);
                    }
                    return true;
                }

                // Handler returned false - treat as failure
                lastException = new InvalidOperationException(
                    $"Handler returned false for message {message.Id}");
                
                _logger.LogWarning(
                    "Handler returned false for message {MessageId} on attempt {Attempt}/{MaxAttempts}",
                    message.Id, attempt, maxAttempts);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex,
                    "Attempt {Attempt}/{MaxAttempts} failed for message {MessageId}: {ErrorMessage}",
                    attempt, maxAttempts, message.Id, ex.Message);
            }

            // If not last attempt, pause and wait before retry
            if (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                var backoff = CalculateBackoff(attempt);

                _logger.LogDebug(
                    "Pausing partition {Partition} for {BackoffMs}ms before retry attempt {NextAttempt}",
                    consumeResult.Partition.Value, 
                    backoff.TotalMilliseconds,
                    attempt + 1);

                // Pause the partition to prevent consuming more messages
                var partition = new TopicPartition(consumeResult.Topic, consumeResult.Partition);
                consumer.Pause(new[] { partition });

                try
                {
                    await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Retry backoff cancelled");
                }
                finally
                {
                    // Always resume, even if cancelled
                    try
                    {
                        consumer.Resume(new[] { partition });
                    }
                    catch (Exception resumeEx)
                    {
                        _logger.LogWarning(resumeEx, 
                            "Error resuming partition {Partition}", 
                            consumeResult.Partition.Value);
                    }
                }
            }
        }

        // All retries exhausted - send to DLT if configured
        if (lastException != null)
        {
            await SendToDeadLetterTopicAsync(consumeResult, message, lastException, attempt, cancellationToken);
        }

        return false;
    }

    private async Task SendToDeadLetterTopicAsync(
        ConsumeResult<string, byte[]> consumeResult,
        T message,
        Exception exception,
        int totalAttempts,
        CancellationToken cancellationToken)
    {
        if (_dltHandler != null)
        {
            var context = new MessageFailureContext<T>
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
                await _dltHandler.OnFailureAsync(context, exception, cancellationToken);
            }
            catch (Exception dltEx)
            {
                _logger.LogCritical(dltEx,
                    "Failed to send message {MessageId} to DLT after {Attempts} attempts. Message may be lost!",
                    message.Id, totalAttempts);
            }
        }
        else
        {
            _logger.LogError(exception,
                "Message {MessageId} failed after {Attempts} attempts. " +
                "No DLT configured - message will be committed and lost.",
                message.Id, totalAttempts);
        }
    }

    /// <summary>
    /// Calculates the backoff delay for a given attempt number.
    /// </summary>
    private TimeSpan CalculateBackoff(int attempt)
    {
        if (!_retryConfig.ExponentialBackoff)
            return _retryConfig.InitialBackoff;

        // Exponential backoff: initialBackoff * 2^(attempt-1)
        var multiplier = Math.Pow(2, attempt - 1);
        var backoff = TimeSpan.FromTicks((long)(_retryConfig.InitialBackoff.Ticks * multiplier));

        // Add jitter (±10%) to prevent thundering herd
        double jitter;
        lock (RandomLock)
        {
            jitter = RandomInstance.NextDouble() * 0.2 - 0.1;
        }
        backoff = TimeSpan.FromTicks((long)(backoff.Ticks * (1 + jitter)));

        // Cap at max backoff
        return backoff > _retryConfig.MaxBackoff ? _retryConfig.MaxBackoff : backoff;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _dltHandler?.Dispose();
        _disposed = true;
    }
}

