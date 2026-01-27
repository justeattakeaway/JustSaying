using System.Security.Cryptography;
using System.Text;
using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Partitioning;

/// <summary>
/// Default partition key strategy using the message ID.
/// This ensures messages with the same ID go to the same partition.
/// </summary>
public class MessageIdPartitionKeyStrategy : IPartitionKeyStrategy
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly MessageIdPartitionKeyStrategy Instance = new();

    /// <inheritdoc />
    public string GetPartitionKey(Message message, string topic)
    {
        return message?.Id.ToString();
    }
}

/// <summary>
/// Partition key strategy using the message's UniqueKey() method.
/// This is the default behavior for KafkaMessagePublisher.
/// </summary>
public class UniqueKeyPartitionKeyStrategy : IPartitionKeyStrategy
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly UniqueKeyPartitionKeyStrategy Instance = new();

    /// <inheritdoc />
    public string GetPartitionKey(Message message, string topic)
    {
        return message?.UniqueKey();
    }
}

/// <summary>
/// Round-robin partition key strategy.
/// Returns null to let Kafka distribute messages across partitions evenly.
/// </summary>
public class RoundRobinPartitionKeyStrategy : IPartitionKeyStrategy
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly RoundRobinPartitionKeyStrategy Instance = new();

    /// <inheritdoc />
    public string GetPartitionKey(Message message, string topic)
    {
        // Return null to let Kafka use its round-robin partitioner
        return null;
    }
}

/// <summary>
/// Sticky partition key strategy.
/// Messages are sent to a random partition that stays "sticky" for a batch or time period.
/// Useful for reducing latency by batching messages to the same partition.
/// </summary>
public class StickyPartitionKeyStrategy : IPartitionKeyStrategy
{
    private readonly TimeSpan _stickyDuration;
    private readonly object _lock = new();
    private string _currentKey;
    private DateTime _lastKeyChange = DateTime.MinValue;

    /// <summary>
    /// Creates a sticky partition key strategy.
    /// </summary>
    /// <param name="stickyDuration">How long to use the same partition before switching. Default is 1 second.</param>
    public StickyPartitionKeyStrategy(TimeSpan? stickyDuration = null)
    {
        _stickyDuration = stickyDuration ?? TimeSpan.FromSeconds(1);
    }

    /// <inheritdoc />
    public string GetPartitionKey(Message message, string topic)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (_currentKey == null || now - _lastKeyChange > _stickyDuration)
            {
                _currentKey = Guid.NewGuid().ToString();
                _lastKeyChange = now;
            }
            return _currentKey;
        }
    }
}

/// <summary>
/// Consistent hashing partition key strategy.
/// Uses a property selector to generate consistent partition keys.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ConsistentHashPartitionKeyStrategy<T> : IPartitionKeyStrategy<T>, IPartitionKeyStrategy 
    where T : Message
{
    private readonly Func<T, string> _keySelector;

    /// <summary>
    /// Creates a consistent hash partition key strategy.
    /// </summary>
    /// <param name="keySelector">Function to select the property to use as the partition key.</param>
    public ConsistentHashPartitionKeyStrategy(Func<T, string> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    /// <inheritdoc />
    public string GetPartitionKey(T message, string topic)
    {
        if (message == null) return null;
        return _keySelector(message);
    }

    /// <inheritdoc />
    string IPartitionKeyStrategy.GetPartitionKey(Message message, string topic)
    {
        if (message is T typedMessage)
        {
            return GetPartitionKey(typedMessage, topic);
        }
        return message?.Id.ToString();
    }
}

/// <summary>
/// Custom partition key strategy using a delegate.
/// </summary>
public class DelegatePartitionKeyStrategy : IPartitionKeyStrategy
{
    private readonly Func<Message, string, string> _keySelector;

    /// <summary>
    /// Creates a delegate-based partition key strategy.
    /// </summary>
    /// <param name="keySelector">Function that takes (message, topic) and returns the partition key.</param>
    public DelegatePartitionKeyStrategy(Func<Message, string, string> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    /// <inheritdoc />
    public string GetPartitionKey(Message message, string topic)
    {
        return _keySelector(message, topic);
    }
}

/// <summary>
/// Typed delegate partition key strategy.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class DelegatePartitionKeyStrategy<T> : IPartitionKeyStrategy<T>, IPartitionKeyStrategy 
    where T : Message
{
    private readonly Func<T, string, string> _keySelector;

    /// <summary>
    /// Creates a typed delegate-based partition key strategy.
    /// </summary>
    /// <param name="keySelector">Function that takes (message, topic) and returns the partition key.</param>
    public DelegatePartitionKeyStrategy(Func<T, string, string> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    /// <inheritdoc />
    public string GetPartitionKey(T message, string topic)
    {
        return _keySelector(message, topic);
    }

    /// <inheritdoc />
    string IPartitionKeyStrategy.GetPartitionKey(Message message, string topic)
    {
        if (message is T typedMessage)
        {
            return GetPartitionKey(typedMessage, topic);
        }
        return message?.Id.ToString();
    }
}

/// <summary>
/// Murmur3-based consistent hash partition key strategy.
/// Provides better distribution than MD5/SHA for partitioning.
/// </summary>
public class Murmur3PartitionKeyStrategy : IPartitionKeyStrategy
{
    private readonly Func<Message, string> _propertySelector;

    /// <summary>
    /// Creates a Murmur3 hash partition key strategy.
    /// </summary>
    /// <param name="propertySelector">Function to select the property to hash.</param>
    public Murmur3PartitionKeyStrategy(Func<Message, string> propertySelector)
    {
        _propertySelector = propertySelector ?? throw new ArgumentNullException(nameof(propertySelector));
    }

    /// <inheritdoc />
    public string GetPartitionKey(Message message, string topic)
    {
        if (message == null) return null;

        var value = _propertySelector(message);
        if (string.IsNullOrEmpty(value)) return null;

        // Use a simple hash for consistency
        var hash = ComputeHash(value);
        return hash.ToString();
    }

    private static uint ComputeHash(string value)
    {
        // Simple Murmur3-like hash
        const uint seed = 0xc58f1a7b;
        const uint c1 = 0xcc9e2d51;
        const uint c2 = 0x1b873593;

        var bytes = Encoding.UTF8.GetBytes(value);
        uint hash = seed;

        foreach (var b in bytes)
        {
            uint k = b;
            k *= c1;
            k = RotateLeft(k, 15);
            k *= c2;

            hash ^= k;
            hash = RotateLeft(hash, 13);
            hash = hash * 5 + 0xe6546b64;
        }

        hash ^= (uint)bytes.Length;
        hash = FMix(hash);

        return hash;
    }

    private static uint RotateLeft(uint x, int r)
    {
        return (x << r) | (x >> (32 - r));
    }

    private static uint FMix(uint h)
    {
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        h ^= h >> 16;
        return h;
    }
}

/// <summary>
/// Time-based partition key strategy.
/// Routes messages to partitions based on their timestamp.
/// Useful for time-series data where you want temporal ordering per partition.
/// </summary>
public class TimeBasedPartitionKeyStrategy : IPartitionKeyStrategy
{
    private readonly TimeSpan _windowSize;

    /// <summary>
    /// Creates a time-based partition key strategy.
    /// </summary>
    /// <param name="windowSize">The time window size. Messages within the same window go to the same partition.</param>
    public TimeBasedPartitionKeyStrategy(TimeSpan windowSize)
    {
        if (windowSize <= TimeSpan.Zero)
            throw new ArgumentException("Window size must be positive", nameof(windowSize));
        _windowSize = windowSize;
    }

    /// <inheritdoc />
    public string GetPartitionKey(Message message, string topic)
    {
        if (message == null) return null;

        var timestamp = message.TimeStamp;
        var windowNumber = timestamp.Ticks / _windowSize.Ticks;
        return windowNumber.ToString();
    }
}
