using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Extensions.Kafka.Partitioning;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Streams;

/// <summary>
/// Handler that executes stream processing operations on messages.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class StreamHandler<T> : IHandlerAsync<T> where T : Message
{
    private readonly string _sourceTopic;
    private readonly string _sinkTopic;
    private readonly KafkaConfiguration _configuration;
    private readonly IReadOnlyList<IStreamOperation> _operations;
    private readonly IPartitionKeyStrategy _sinkPartitionStrategy;

    private IMessagePublisher _publisher;
    private ILogger _logger;

    /// <summary>
    /// Creates a new stream handler.
    /// </summary>
    internal StreamHandler(
        string sourceTopic,
        string sinkTopic,
        KafkaConfiguration configuration,
        IReadOnlyList<IStreamOperation> operations,
        IPartitionKeyStrategy sinkPartitionStrategy)
    {
        _sourceTopic = sourceTopic;
        _sinkTopic = sinkTopic;
        _configuration = configuration;
        _operations = operations;
        _sinkPartitionStrategy = sinkPartitionStrategy;
    }

    /// <summary>
    /// Initializes the handler with required services.
    /// </summary>
    public void Initialize(IMessagePublisher publisher, ILoggerFactory loggerFactory)
    {
        _publisher = publisher;
        _logger = loggerFactory?.CreateLogger<StreamHandler<T>>();
    }

    /// <inheritdoc />
    public async Task<bool> Handle(T message)
    {
        var context = new StreamContext
        {
            Topic = _sourceTopic,
            Timestamp = message.TimeStamp,
            OutputTopic = _sinkTopic
        };

        try
        {
            // Process through the operation pipeline
            var results = await ProcessPipelineAsync(message, context, CancellationToken.None)
                .ConfigureAwait(false);

            // Publish results to sink topic if configured
            if (!string.IsNullOrEmpty(_sinkTopic) && _publisher != null)
            {
                foreach (var result in results)
                {
                    await _publisher.PublishAsync(result, CancellationToken.None).ConfigureAwait(false);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Stream processing failed for message {MessageId}", message.Id);
            return false;
        }
    }

    private async Task<IEnumerable<Message>> ProcessPipelineAsync(
        T message,
        StreamContext context,
        CancellationToken cancellationToken)
    {
        object current = message;
        var currentType = typeof(T);

        foreach (var operation in _operations)
        {
            if (current == null) break;

            switch (operation)
            {
                case FilterOperation<T> filter when current is T typedMessage:
                    if (!filter.Apply(typedMessage, context))
                        return Enumerable.Empty<Message>();
                    break;

                case PeekOperation<T> peek when current is T typedMessage:
                    peek.Apply(typedMessage, context);
                    break;

                case PeekAsyncOperation<T> peekAsync when current is T typedMessage:
                    await peekAsync.ApplyAsync(typedMessage, context, cancellationToken)
                        .ConfigureAwait(false);
                    break;

                case BranchOperation<T> branch when current is T typedMessage:
                    // Branch returns multiple (message, topic) pairs
                    var branchResults = new List<Message>();
                    foreach (var (msg, topic) in branch.Apply(typedMessage))
                    {
                        // Set target topic in context
                        context.OutputTopic = topic;
                        branchResults.Add(msg);
                    }
                    return branchResults;

                default:
                    // Handle generic operations through reflection
                    var result = ApplyOperationDynamic(operation, current, context);
                    if (result == null)
                        return Enumerable.Empty<Message>();
                    current = result;
                    break;
            }
        }

        if (current is Message resultMessage)
            return new[] { resultMessage };

        if (current is IEnumerable<Message> messages)
            return messages;

        return Enumerable.Empty<Message>();
    }

    private object ApplyOperationDynamic(IStreamOperation operation, object current, StreamContext context)
    {
        var operationType = operation.GetType();

        // Try to find and invoke the Apply method
        var applyMethod = operationType.GetMethod("Apply");
        if (applyMethod != null)
        {
            return applyMethod.Invoke(operation, new[] { current, context });
        }

        return current;
    }

    /// <summary>
    /// Gets the source topic.
    /// </summary>
    public string SourceTopic => _sourceTopic;

    /// <summary>
    /// Gets the sink topic.
    /// </summary>
    public string SinkTopic => _sinkTopic;

    /// <summary>
    /// Gets the operations in this stream.
    /// </summary>
    public IReadOnlyList<IStreamOperation> Operations => _operations;
}
