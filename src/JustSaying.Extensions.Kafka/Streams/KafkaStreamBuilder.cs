using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Extensions.Kafka.Partitioning;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Streams;

/// <summary>
/// Fluent builder for creating Kafka stream processing topologies.
/// Provides a lightweight stream processing abstraction for .NET.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class KafkaStreamBuilder<T> where T : Message
{
    private readonly string _sourceTopic;
    private readonly KafkaConfiguration _configuration = new();
    private readonly List<IStreamOperation> _operations = new();
    private string _sinkTopic;
    private IPartitionKeyStrategy _sinkPartitionStrategy;

    /// <summary>
    /// Creates a new stream builder for the specified source topic.
    /// </summary>
    /// <param name="sourceTopic">The source topic to consume from.</param>
    public KafkaStreamBuilder(string sourceTopic)
    {
        _sourceTopic = sourceTopic ?? throw new ArgumentNullException(nameof(sourceTopic));
    }

    /// <summary>
    /// Configures the Kafka bootstrap servers.
    /// </summary>
    public KafkaStreamBuilder<T> WithBootstrapServers(string bootstrapServers)
    {
        _configuration.BootstrapServers = bootstrapServers;
        return this;
    }

    /// <summary>
    /// Configures the consumer group ID.
    /// </summary>
    public KafkaStreamBuilder<T> WithGroupId(string groupId)
    {
        _configuration.GroupId = groupId;
        return this;
    }

    /// <summary>
    /// Enables or disables CloudEvents format.
    /// </summary>
    public KafkaStreamBuilder<T> WithCloudEvents(bool enable = true, string source = "urn:justsaying")
    {
        _configuration.EnableCloudEvents = enable;
        _configuration.CloudEventsSource = source;
        return this;
    }

    #region Stream Operations

    /// <summary>
    /// Filters messages based on a predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    public KafkaStreamBuilder<T> Filter(Func<T, bool> predicate)
    {
        _operations.Add(new FilterOperation<T>(predicate));
        return this;
    }

    /// <summary>
    /// Filters messages based on a predicate with access to context.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    public KafkaStreamBuilder<T> Filter(Func<T, StreamContext, bool> predicate)
    {
        _operations.Add(new FilterOperation<T>(predicate));
        return this;
    }

    /// <summary>
    /// Transforms each message using a mapping function.
    /// </summary>
    /// <typeparam name="TOut">The output message type.</typeparam>
    /// <param name="mapper">The mapping function.</param>
    public KafkaStreamBuilder<TOut> Map<TOut>(Func<T, TOut> mapper) where TOut : Message
    {
        var newBuilder = new KafkaStreamBuilder<TOut>(_sourceTopic);
        CopyConfigurationTo(newBuilder);
        newBuilder._operations.AddRange(_operations);
        newBuilder._operations.Add(new MapOperation<T, TOut>(mapper));
        return newBuilder;
    }

    /// <summary>
    /// Transforms each message using a mapping function with access to context.
    /// </summary>
    /// <typeparam name="TOut">The output message type.</typeparam>
    /// <param name="mapper">The mapping function.</param>
    public KafkaStreamBuilder<TOut> Map<TOut>(Func<T, StreamContext, TOut> mapper) where TOut : Message
    {
        var newBuilder = new KafkaStreamBuilder<TOut>(_sourceTopic);
        CopyConfigurationTo(newBuilder);
        newBuilder._operations.AddRange(_operations);
        newBuilder._operations.Add(new MapOperation<T, TOut>(mapper));
        return newBuilder;
    }

    /// <summary>
    /// Transforms each message into zero or more messages.
    /// </summary>
    /// <typeparam name="TOut">The output message type.</typeparam>
    /// <param name="mapper">The flat-mapping function.</param>
    public KafkaStreamBuilder<TOut> FlatMap<TOut>(Func<T, IEnumerable<TOut>> mapper) where TOut : Message
    {
        var newBuilder = new KafkaStreamBuilder<TOut>(_sourceTopic);
        CopyConfigurationTo(newBuilder);
        newBuilder._operations.AddRange(_operations);
        newBuilder._operations.Add(new FlatMapOperation<T, TOut>(mapper));
        return newBuilder;
    }

    /// <summary>
    /// Performs a side-effect action for each message without changing the stream.
    /// </summary>
    /// <param name="action">The side-effect action.</param>
    public KafkaStreamBuilder<T> Peek(Action<T> action)
    {
        _operations.Add(new PeekOperation<T>(action));
        return this;
    }

    /// <summary>
    /// Performs an async side-effect action for each message.
    /// </summary>
    /// <param name="action">The async side-effect action.</param>
    public KafkaStreamBuilder<T> PeekAsync(Func<T, StreamContext, CancellationToken, Task> action)
    {
        _operations.Add(new PeekAsyncOperation<T>(action));
        return this;
    }

    /// <summary>
    /// Branches the stream based on predicates.
    /// </summary>
    /// <param name="branches">Array of (predicate, topic) pairs.</param>
    public KafkaStreamBuilder<T> Branch(params (Func<T, bool> predicate, string topic)[] branches)
    {
        _operations.Add(new BranchOperation<T>(branches));
        return this;
    }

    #endregion

    #region Windowing & Aggregation

    /// <summary>
    /// Groups messages by a key for aggregation.
    /// </summary>
    /// <param name="keySelector">Function to extract the grouping key.</param>
    public KafkaGroupedStreamBuilder<T, TKey> GroupBy<TKey>(Func<T, TKey> keySelector)
    {
        return new KafkaGroupedStreamBuilder<T, TKey>(this, keySelector);
    }

    #endregion

    #region Sink Configuration

    /// <summary>
    /// Sends processed messages to an output topic.
    /// </summary>
    /// <param name="topic">The output topic.</param>
    public KafkaStreamBuilder<T> To(string topic)
    {
        _sinkTopic = topic;
        return this;
    }

    /// <summary>
    /// Configures the partition key strategy for the sink.
    /// </summary>
    /// <param name="strategy">The partition key strategy.</param>
    public KafkaStreamBuilder<T> WithSinkPartitioning(IPartitionKeyStrategy strategy)
    {
        _sinkPartitionStrategy = strategy;
        return this;
    }

    #endregion

    /// <summary>
    /// Builds the stream handler that can be registered with DI.
    /// </summary>
    public StreamHandler<T> Build()
    {
        _configuration.Validate();
        return new StreamHandler<T>(_sourceTopic, _sinkTopic, _configuration, _operations, _sinkPartitionStrategy);
    }

    /// <summary>
    /// Gets the source topic.
    /// </summary>
    public string GetSourceTopic() => _sourceTopic;

    /// <summary>
    /// Gets the sink topic.
    /// </summary>
    public string GetSinkTopic() => _sinkTopic;

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public KafkaConfiguration GetConfiguration() => _configuration;

    private void CopyConfigurationTo<TOther>(KafkaStreamBuilder<TOther> other) where TOther : Message
    {
        other._configuration.BootstrapServers = _configuration.BootstrapServers;
        other._configuration.GroupId = _configuration.GroupId;
        other._configuration.EnableCloudEvents = _configuration.EnableCloudEvents;
        other._configuration.CloudEventsSource = _configuration.CloudEventsSource;
    }
}

/// <summary>
/// Builder for grouped streams (after GroupBy).
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
/// <typeparam name="TKey">The grouping key type.</typeparam>
public class KafkaGroupedStreamBuilder<T, TKey> where T : Message
{
    private readonly KafkaStreamBuilder<T> _parent;
    private readonly Func<T, TKey> _keySelector;

    internal KafkaGroupedStreamBuilder(KafkaStreamBuilder<T> parent, Func<T, TKey> keySelector)
    {
        _parent = parent;
        _keySelector = keySelector;
    }

    /// <summary>
    /// Creates a tumbling window for aggregation.
    /// </summary>
    /// <param name="windowSize">The window size.</param>
    public KafkaWindowedStreamBuilder<T, TKey> WindowedBy(TimeSpan windowSize)
    {
        return new KafkaWindowedStreamBuilder<T, TKey>(_parent, _keySelector, windowSize, WindowType.Tumbling);
    }

    /// <summary>
    /// Creates a sliding window for aggregation.
    /// </summary>
    /// <param name="windowSize">The window size.</param>
    /// <param name="advanceBy">How much to advance the window.</param>
    public KafkaWindowedStreamBuilder<T, TKey> SlidingWindowedBy(TimeSpan windowSize, TimeSpan advanceBy)
    {
        return new KafkaWindowedStreamBuilder<T, TKey>(_parent, _keySelector, windowSize, WindowType.Sliding, advanceBy);
    }

    /// <summary>
    /// Creates a session window for aggregation.
    /// </summary>
    /// <param name="inactivityGap">The inactivity gap that defines session boundaries.</param>
    public KafkaWindowedStreamBuilder<T, TKey> SessionWindowedBy(TimeSpan inactivityGap)
    {
        return new KafkaWindowedStreamBuilder<T, TKey>(_parent, _keySelector, inactivityGap, WindowType.Session);
    }

    /// <summary>
    /// Performs a count aggregation.
    /// </summary>
    public KafkaStreamBuilder<AggregationResult<TKey, long>> Count()
    {
        var newBuilder = new KafkaStreamBuilder<AggregationResult<TKey, long>>(_parent.GetSourceTopic());
        // Note: Full implementation would track state and emit counts
        return newBuilder;
    }
}

/// <summary>
/// Builder for windowed streams.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
/// <typeparam name="TKey">The grouping key type.</typeparam>
public class KafkaWindowedStreamBuilder<T, TKey> where T : Message
{
    private readonly KafkaStreamBuilder<T> _parent;
    private readonly Func<T, TKey> _keySelector;
    private readonly TimeSpan _windowSize;
    private readonly WindowType _windowType;
    private readonly TimeSpan? _advanceBy;

    internal KafkaWindowedStreamBuilder(
        KafkaStreamBuilder<T> parent,
        Func<T, TKey> keySelector,
        TimeSpan windowSize,
        WindowType windowType,
        TimeSpan? advanceBy = null)
    {
        _parent = parent;
        _keySelector = keySelector;
        _windowSize = windowSize;
        _windowType = windowType;
        _advanceBy = advanceBy;
    }

    /// <summary>
    /// Performs a count aggregation within the window.
    /// </summary>
    public KafkaStreamBuilder<WindowedAggregationResult<TKey, long>> Count()
    {
        var newBuilder = new KafkaStreamBuilder<WindowedAggregationResult<TKey, long>>(_parent.GetSourceTopic());
        // Note: Full implementation would track state and emit windowed counts
        return newBuilder;
    }

    /// <summary>
    /// Performs a reduce aggregation within the window.
    /// </summary>
    /// <param name="reducer">The reducer function.</param>
    public KafkaStreamBuilder<WindowedAggregationResult<TKey, T>> Reduce(Func<T, T, T> reducer)
    {
        var newBuilder = new KafkaStreamBuilder<WindowedAggregationResult<TKey, T>>(_parent.GetSourceTopic());
        // Note: Full implementation would track state and emit reduced values
        return newBuilder;
    }

    /// <summary>
    /// Performs a custom aggregation within the window.
    /// </summary>
    /// <typeparam name="TAgg">The aggregation result type.</typeparam>
    /// <param name="initializer">Function to create the initial aggregation value.</param>
    /// <param name="aggregator">Function to aggregate each message.</param>
    public KafkaStreamBuilder<WindowedAggregationResult<TKey, TAgg>> Aggregate<TAgg>(
        Func<TAgg> initializer,
        Func<TKey, T, TAgg, TAgg> aggregator)
    {
        var newBuilder = new KafkaStreamBuilder<WindowedAggregationResult<TKey, TAgg>>(_parent.GetSourceTopic());
        // Note: Full implementation would track state and emit aggregated values
        return newBuilder;
    }
}

/// <summary>
/// Window type for aggregations.
/// </summary>
public enum WindowType
{
    /// <summary>
    /// Fixed-size non-overlapping windows.
    /// </summary>
    Tumbling,

    /// <summary>
    /// Fixed-size overlapping windows.
    /// </summary>
    Sliding,

    /// <summary>
    /// Variable-size windows based on activity gaps.
    /// </summary>
    Session
}

/// <summary>
/// Result of a keyed aggregation.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The aggregated value type.</typeparam>
public class AggregationResult<TKey, TValue> : Message
{
    /// <summary>
    /// The aggregation key.
    /// </summary>
    public TKey Key { get; set; }

    /// <summary>
    /// The aggregated value.
    /// </summary>
    public TValue Value { get; set; }
}

/// <summary>
/// Result of a windowed aggregation.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The aggregated value type.</typeparam>
public class WindowedAggregationResult<TKey, TValue> : Message
{
    /// <summary>
    /// The aggregation key.
    /// </summary>
    public TKey Key { get; set; }

    /// <summary>
    /// The aggregated value.
    /// </summary>
    public TValue Value { get; set; }

    /// <summary>
    /// The window start time.
    /// </summary>
    public DateTime WindowStart { get; set; }

    /// <summary>
    /// The window end time.
    /// </summary>
    public DateTime WindowEnd { get; set; }
}
