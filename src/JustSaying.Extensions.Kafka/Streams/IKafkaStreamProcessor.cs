using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Streams;

/// <summary>
/// Interface for processing Kafka messages in a stream-like manner.
/// Provides a lightweight alternative to Kafka Streams for .NET.
/// </summary>
/// <typeparam name="TIn">The input message type.</typeparam>
/// <typeparam name="TOut">The output message type (can be same as input for transformations).</typeparam>
public interface IKafkaStreamProcessor<TIn, TOut>
    where TIn : Message
    where TOut : Message
{
    /// <summary>
    /// Processes an input message and produces zero or more output messages.
    /// </summary>
    /// <param name="input">The input message.</param>
    /// <param name="context">The stream context with metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The output messages to forward downstream.</returns>
    Task<IEnumerable<TOut>> ProcessAsync(TIn input, StreamContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context provided to stream processors with message metadata.
/// </summary>
public class StreamContext
{
    /// <summary>
    /// The source topic.
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// The partition the message came from.
    /// </summary>
    public int Partition { get; set; }

    /// <summary>
    /// The offset of the message.
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// The message key.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The message timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Message headers.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; set; }

    /// <summary>
    /// The output topic (for processors that forward to another topic).
    /// </summary>
    public string OutputTopic { get; set; }

    /// <summary>
    /// Custom state that can be passed between processors.
    /// </summary>
    public IDictionary<string, object> State { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// A simple transformation processor that applies a function to each message.
/// </summary>
/// <typeparam name="TIn">The input message type.</typeparam>
/// <typeparam name="TOut">The output message type.</typeparam>
public class MapProcessor<TIn, TOut> : IKafkaStreamProcessor<TIn, TOut>
    where TIn : Message
    where TOut : Message
{
    private readonly Func<TIn, StreamContext, TOut> _mapper;

    /// <summary>
    /// Creates a map processor.
    /// </summary>
    /// <param name="mapper">The mapping function.</param>
    public MapProcessor(Func<TIn, StreamContext, TOut> mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <inheritdoc />
    public Task<IEnumerable<TOut>> ProcessAsync(TIn input, StreamContext context, CancellationToken cancellationToken = default)
    {
        var result = _mapper(input, context);
        return Task.FromResult<IEnumerable<TOut>>(result != null ? new[] { result } : Array.Empty<TOut>());
    }
}

/// <summary>
/// A filter processor that only forwards messages matching a predicate.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class FilterProcessor<T> : IKafkaStreamProcessor<T, T> where T : Message
{
    private readonly Func<T, StreamContext, bool> _predicate;

    /// <summary>
    /// Creates a filter processor.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    public FilterProcessor(Func<T, StreamContext, bool> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    /// <inheritdoc />
    public Task<IEnumerable<T>> ProcessAsync(T input, StreamContext context, CancellationToken cancellationToken = default)
    {
        var result = _predicate(input, context) ? new[] { input } : Array.Empty<T>();
        return Task.FromResult<IEnumerable<T>>(result);
    }
}

/// <summary>
/// A flat-map processor that can produce zero or more messages per input.
/// </summary>
/// <typeparam name="TIn">The input message type.</typeparam>
/// <typeparam name="TOut">The output message type.</typeparam>
public class FlatMapProcessor<TIn, TOut> : IKafkaStreamProcessor<TIn, TOut>
    where TIn : Message
    where TOut : Message
{
    private readonly Func<TIn, StreamContext, IEnumerable<TOut>> _mapper;

    /// <summary>
    /// Creates a flat-map processor.
    /// </summary>
    /// <param name="mapper">The mapping function that returns multiple outputs.</param>
    public FlatMapProcessor(Func<TIn, StreamContext, IEnumerable<TOut>> mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <inheritdoc />
    public Task<IEnumerable<TOut>> ProcessAsync(TIn input, StreamContext context, CancellationToken cancellationToken = default)
    {
        var results = _mapper(input, context) ?? Enumerable.Empty<TOut>();
        return Task.FromResult(results);
    }
}

/// <summary>
/// An async processor for more complex transformations.
/// </summary>
/// <typeparam name="TIn">The input message type.</typeparam>
/// <typeparam name="TOut">The output message type.</typeparam>
public class AsyncProcessor<TIn, TOut> : IKafkaStreamProcessor<TIn, TOut>
    where TIn : Message
    where TOut : Message
{
    private readonly Func<TIn, StreamContext, CancellationToken, Task<IEnumerable<TOut>>> _processor;

    /// <summary>
    /// Creates an async processor.
    /// </summary>
    /// <param name="processor">The async processing function.</param>
    public AsyncProcessor(Func<TIn, StreamContext, CancellationToken, Task<IEnumerable<TOut>>> processor)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
    }

    /// <inheritdoc />
    public Task<IEnumerable<TOut>> ProcessAsync(TIn input, StreamContext context, CancellationToken cancellationToken = default)
    {
        return _processor(input, context, cancellationToken);
    }
}
