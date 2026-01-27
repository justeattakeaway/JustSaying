using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Streams;

/// <summary>
/// Base interface for stream operations.
/// </summary>
public interface IStreamOperation
{
    /// <summary>
    /// Gets the operation type for serialization/debugging.
    /// </summary>
    string OperationType { get; }
}

/// <summary>
/// Filter operation that removes messages not matching a predicate.
/// </summary>
internal class FilterOperation<T> : IStreamOperation where T : Message
{
    private readonly Func<T, StreamContext, bool> _predicate;

    public string OperationType => "Filter";

    public FilterOperation(Func<T, bool> predicate)
    {
        _predicate = (msg, _) => predicate(msg);
    }

    public FilterOperation(Func<T, StreamContext, bool> predicate)
    {
        _predicate = predicate;
    }

    public bool Apply(T message, StreamContext context) => _predicate(message, context);
}

/// <summary>
/// Map operation that transforms messages.
/// </summary>
internal class MapOperation<TIn, TOut> : IStreamOperation
    where TIn : Message
    where TOut : Message
{
    private readonly Func<TIn, StreamContext, TOut> _mapper;

    public string OperationType => "Map";

    public MapOperation(Func<TIn, TOut> mapper)
    {
        _mapper = (msg, _) => mapper(msg);
    }

    public MapOperation(Func<TIn, StreamContext, TOut> mapper)
    {
        _mapper = mapper;
    }

    public TOut Apply(TIn message, StreamContext context) => _mapper(message, context);
}

/// <summary>
/// Flat-map operation that transforms messages into multiple outputs.
/// </summary>
internal class FlatMapOperation<TIn, TOut> : IStreamOperation
    where TIn : Message
    where TOut : Message
{
    private readonly Func<TIn, StreamContext, IEnumerable<TOut>> _mapper;

    public string OperationType => "FlatMap";

    public FlatMapOperation(Func<TIn, IEnumerable<TOut>> mapper)
    {
        _mapper = (msg, _) => mapper(msg);
    }

    public FlatMapOperation(Func<TIn, StreamContext, IEnumerable<TOut>> mapper)
    {
        _mapper = mapper;
    }

    public IEnumerable<TOut> Apply(TIn message, StreamContext context) => _mapper(message, context);
}

/// <summary>
/// Peek operation for side effects without changing the stream.
/// </summary>
internal class PeekOperation<T> : IStreamOperation where T : Message
{
    private readonly Action<T, StreamContext> _action;

    public string OperationType => "Peek";

    public PeekOperation(Action<T> action)
    {
        _action = (msg, _) => action(msg);
    }

    public PeekOperation(Action<T, StreamContext> action)
    {
        _action = action;
    }

    public void Apply(T message, StreamContext context) => _action(message, context);
}

/// <summary>
/// Async peek operation for side effects.
/// </summary>
internal class PeekAsyncOperation<T> : IStreamOperation where T : Message
{
    private readonly Func<T, StreamContext, CancellationToken, Task> _action;

    public string OperationType => "PeekAsync";

    public PeekAsyncOperation(Func<T, StreamContext, CancellationToken, Task> action)
    {
        _action = action;
    }

    public Task ApplyAsync(T message, StreamContext context, CancellationToken cancellationToken)
        => _action(message, context, cancellationToken);
}

/// <summary>
/// Branch operation that routes messages to different topics based on predicates.
/// </summary>
internal class BranchOperation<T> : IStreamOperation where T : Message
{
    private readonly (Func<T, bool> predicate, string topic)[] _branches;

    public string OperationType => "Branch";

    public BranchOperation((Func<T, bool> predicate, string topic)[] branches)
    {
        _branches = branches;
    }

    public string GetTargetTopic(T message)
    {
        foreach (var (predicate, topic) in _branches)
        {
            if (predicate(message))
                return topic;
        }
        return null; // No matching branch
    }

    public IEnumerable<(T message, string topic)> Apply(T message)
    {
        foreach (var (predicate, topic) in _branches)
        {
            if (predicate(message))
            {
                yield return (message, topic);
            }
        }
    }
}
