using JustSaying.Extensions.Kafka.Messaging;

namespace JustSaying.Extensions.Kafka.Tests.Messaging;

public class KafkaMessageContextTests
{
    [Fact]
    public void Properties_SetAndGet_WorkCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddSeconds(-5);
        var receivedAt = DateTime.UtcNow;
        var headers = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var context = new KafkaMessageContext
        {
            Topic = "test-topic",
            Partition = 3,
            Offset = 12345,
            Key = "partition-key",
            Timestamp = timestamp,
            Headers = headers,
            ReceivedAt = receivedAt,
            GroupId = "test-group",
            ConsumerId = "consumer-1",
            RetryAttempt = 2,
            CloudEventType = "OrderPlaced",
            CloudEventSource = "//orders/service",
            CloudEventId = "event-123"
        };

        // Assert
        Assert.Equal("test-topic", context.Topic);
        Assert.Equal(3, context.Partition);
        Assert.Equal(12345, context.Offset);
        Assert.Equal("partition-key", context.Key);
        Assert.Equal(timestamp, context.Timestamp);
        Assert.Equal(headers, context.Headers);
        Assert.Equal(receivedAt, context.ReceivedAt);
        Assert.Equal("test-group", context.GroupId);
        Assert.Equal("consumer-1", context.ConsumerId);
        Assert.Equal(2, context.RetryAttempt);
        Assert.Equal("OrderPlaced", context.CloudEventType);
        Assert.Equal("//orders/service", context.CloudEventSource);
        Assert.Equal("event-123", context.CloudEventId);
    }

    [Fact]
    public void LagMilliseconds_CalculatesCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddMilliseconds(-500);
        var receivedAt = DateTime.UtcNow;

        var context = new KafkaMessageContext
        {
            Timestamp = timestamp,
            ReceivedAt = receivedAt
        };

        // Act
        var lag = context.LagMilliseconds;

        // Assert
        Assert.InRange(lag, 490, 510); // Allow for some tolerance
    }

    [Fact]
    public void LagMilliseconds_NegativeWhenReceivedBeforeTimestamp()
    {
        // Arrange (edge case: clock skew)
        var timestamp = DateTime.UtcNow.AddMilliseconds(100);
        var receivedAt = DateTime.UtcNow;

        var context = new KafkaMessageContext
        {
            Timestamp = timestamp,
            ReceivedAt = receivedAt
        };

        // Act
        var lag = context.LagMilliseconds;

        // Assert
        Assert.True(lag < 0);
    }

    [Fact]
    public void Headers_DefaultToEmptyDictionary()
    {
        // Arrange
        var context = new KafkaMessageContext();

        // Assert
        Assert.NotNull(context.Headers);
        Assert.Empty(context.Headers);
    }

    [Fact]
    public void RetryAttempt_DefaultsToZero()
    {
        // Arrange
        var context = new KafkaMessageContext();

        // Assert
        Assert.Equal(0, context.RetryAttempt);
    }
}

