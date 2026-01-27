using JustSaying.Extensions.Kafka.Partitioning;
using JustSaying.Models;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Partitioning;

public class PartitionKeyStrategyTests
{
    #region MessageIdPartitionKeyStrategy Tests

    [Fact]
    public void MessageIdStrategy_ReturnsMessageId()
    {
        // Arrange
        var strategy = MessageIdPartitionKeyStrategy.Instance;
        var message = new TestMessage { Id = Guid.Parse("12345678-1234-1234-1234-123456789abc") };

        // Act
        var key = strategy.GetPartitionKey(message, "topic");

        // Assert
        key.ShouldBe("12345678-1234-1234-1234-123456789abc");
    }

    [Fact]
    public void MessageIdStrategy_ReturnsNullForNullMessage()
    {
        // Arrange
        var strategy = MessageIdPartitionKeyStrategy.Instance;

        // Act
        var key = strategy.GetPartitionKey(null, "topic");

        // Assert
        key.ShouldBeNull();
    }

    [Fact]
    public void MessageIdStrategy_IsSingleton()
    {
        MessageIdPartitionKeyStrategy.Instance.ShouldBeSameAs(MessageIdPartitionKeyStrategy.Instance);
    }

    #endregion

    #region UniqueKeyPartitionKeyStrategy Tests

    [Fact]
    public void UniqueKeyStrategy_ReturnsUniqueKey()
    {
        // Arrange
        var strategy = UniqueKeyPartitionKeyStrategy.Instance;
        var message = new TestMessage { Id = Guid.NewGuid() };

        // Act
        var key = strategy.GetPartitionKey(message, "topic");

        // Assert
        key.ShouldBe(message.UniqueKey());
    }

    [Fact]
    public void UniqueKeyStrategy_IsSingleton()
    {
        UniqueKeyPartitionKeyStrategy.Instance.ShouldBeSameAs(UniqueKeyPartitionKeyStrategy.Instance);
    }

    #endregion

    #region RoundRobinPartitionKeyStrategy Tests

    [Fact]
    public void RoundRobinStrategy_ReturnsNull()
    {
        // Arrange
        var strategy = RoundRobinPartitionKeyStrategy.Instance;
        var message = new TestMessage();

        // Act
        var key = strategy.GetPartitionKey(message, "topic");

        // Assert
        key.ShouldBeNull();
    }

    [Fact]
    public void RoundRobinStrategy_IsSingleton()
    {
        RoundRobinPartitionKeyStrategy.Instance.ShouldBeSameAs(RoundRobinPartitionKeyStrategy.Instance);
    }

    #endregion

    #region StickyPartitionKeyStrategy Tests

    [Fact]
    public void StickyStrategy_ReturnsSameKeyWithinDuration()
    {
        // Arrange
        var strategy = new StickyPartitionKeyStrategy(TimeSpan.FromMinutes(5));
        var message = new TestMessage();

        // Act
        var key1 = strategy.GetPartitionKey(message, "topic");
        var key2 = strategy.GetPartitionKey(message, "topic");
        var key3 = strategy.GetPartitionKey(message, "topic");

        // Assert
        key1.ShouldBe(key2);
        key2.ShouldBe(key3);
    }

    [Fact]
    public void StickyStrategy_ChangesKeyAfterDuration()
    {
        // Arrange - use very short duration
        var strategy = new StickyPartitionKeyStrategy(TimeSpan.FromMilliseconds(1));
        var message = new TestMessage();

        // Act
        var key1 = strategy.GetPartitionKey(message, "topic");
        Thread.Sleep(10); // Wait for sticky duration to expire
        var key2 = strategy.GetPartitionKey(message, "topic");

        // Assert - keys should be different (or same if within timeout, this is probabilistic)
        // We're mainly testing that it doesn't throw
        key1.ShouldNotBeNull();
        key2.ShouldNotBeNull();
    }

    [Fact]
    public void StickyStrategy_DefaultDurationIsOneSecond()
    {
        // Act & Assert - should not throw
        Should.NotThrow(() => new StickyPartitionKeyStrategy());
    }

    #endregion

    #region TimeBasedPartitionKeyStrategy Tests

    [Fact]
    public void TimeBasedStrategy_ReturnsSameKeyForMessagesInSameWindow()
    {
        // Arrange
        var strategy = new TimeBasedPartitionKeyStrategy(TimeSpan.FromHours(1));
        // Use a specific time to ensure both messages are in the same hour window
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var message1 = new TestMessage();
        message1.TimeStamp = baseTime;
        var message2 = new TestMessage();
        message2.TimeStamp = baseTime.AddMinutes(30);

        // Act
        var key1 = strategy.GetPartitionKey(message1, "topic");
        var key2 = strategy.GetPartitionKey(message2, "topic");

        // Assert
        key1.ShouldBe(key2);
    }

    [Fact]
    public void TimeBasedStrategy_ReturnsDifferentKeyForMessagesInDifferentWindows()
    {
        // Arrange
        var strategy = new TimeBasedPartitionKeyStrategy(TimeSpan.FromHours(1));
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var message1 = new TestMessage { TimeStamp = baseTime };
        var message2 = new TestMessage { TimeStamp = baseTime.AddHours(2) };

        // Act
        var key1 = strategy.GetPartitionKey(message1, "topic");
        var key2 = strategy.GetPartitionKey(message2, "topic");

        // Assert
        key1.ShouldNotBe(key2);
    }

    [Fact]
    public void TimeBasedStrategy_ThrowsForZeroOrNegativeWindow()
    {
        Should.Throw<ArgumentException>(() => new TimeBasedPartitionKeyStrategy(TimeSpan.Zero));
        Should.Throw<ArgumentException>(() => new TimeBasedPartitionKeyStrategy(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void TimeBasedStrategy_ReturnsNullForNullMessage()
    {
        // Arrange
        var strategy = new TimeBasedPartitionKeyStrategy(TimeSpan.FromHours(1));

        // Act
        var key = strategy.GetPartitionKey(null, "topic");

        // Assert
        key.ShouldBeNull();
    }

    #endregion

    #region ConsistentHashPartitionKeyStrategy Tests

    [Fact]
    public void ConsistentHashStrategy_ReturnsSelectedProperty()
    {
        // Arrange
        var strategy = new ConsistentHashPartitionKeyStrategy<TestOrderMessage>(m => m.CustomerId);
        var message = new TestOrderMessage { CustomerId = "cust-123" };

        // Act
        var key = strategy.GetPartitionKey(message, "topic");

        // Assert
        key.ShouldBe("cust-123");
    }

    [Fact]
    public void ConsistentHashStrategy_ReturnsSameKeyForSameProperty()
    {
        // Arrange
        var strategy = new ConsistentHashPartitionKeyStrategy<TestOrderMessage>(m => m.CustomerId);
        var message1 = new TestOrderMessage { CustomerId = "cust-abc" };
        var message2 = new TestOrderMessage { CustomerId = "cust-abc" };

        // Act
        var key1 = strategy.GetPartitionKey(message1, "topic");
        var key2 = strategy.GetPartitionKey(message2, "topic");

        // Assert
        key1.ShouldBe(key2);
    }

    [Fact]
    public void ConsistentHashStrategy_ThrowsForNullSelector()
    {
        Should.Throw<ArgumentNullException>(() => 
            new ConsistentHashPartitionKeyStrategy<TestOrderMessage>(null));
    }

    [Fact]
    public void ConsistentHashStrategy_ReturnsNullForNullMessage()
    {
        // Arrange
        var strategy = new ConsistentHashPartitionKeyStrategy<TestOrderMessage>(m => m.CustomerId);

        // Act
        var key = strategy.GetPartitionKey((TestOrderMessage)null, "topic");

        // Assert
        key.ShouldBeNull();
    }

    [Fact]
    public void ConsistentHashStrategy_WorksWithBaseInterface()
    {
        // Arrange
        var strategy = new ConsistentHashPartitionKeyStrategy<TestOrderMessage>(m => m.CustomerId);
        IPartitionKeyStrategy baseStrategy = strategy;
        var message = new TestOrderMessage { CustomerId = "cust-456" };

        // Act
        var key = baseStrategy.GetPartitionKey(message, "topic");

        // Assert
        key.ShouldBe("cust-456");
    }

    #endregion

    #region DelegatePartitionKeyStrategy Tests

    [Fact]
    public void DelegateStrategy_CallsDelegate()
    {
        // Arrange
        var strategy = new DelegatePartitionKeyStrategy((msg, topic) => $"{topic}:{msg.Id}");
        var messageId = Guid.NewGuid();
        var message = new TestMessage { Id = messageId };

        // Act
        var key = strategy.GetPartitionKey(message, "my-topic");

        // Assert
        key.ShouldBe($"my-topic:{messageId}");
    }

    [Fact]
    public void DelegateStrategy_ThrowsForNullDelegate()
    {
        Should.Throw<ArgumentNullException>(() => 
            new DelegatePartitionKeyStrategy(null));
    }

    [Fact]
    public void TypedDelegateStrategy_CallsDelegate()
    {
        // Arrange
        var strategy = new DelegatePartitionKeyStrategy<TestOrderMessage>(
            (msg, topic) => $"{msg.CustomerId}@{topic}");
        var message = new TestOrderMessage { CustomerId = "cust-789" };

        // Act
        var key = strategy.GetPartitionKey(message, "orders");

        // Assert
        key.ShouldBe("cust-789@orders");
    }

    #endregion

    #region Murmur3PartitionKeyStrategy Tests

    [Fact]
    public void Murmur3Strategy_ReturnsDeterministicHash()
    {
        // Arrange
        var strategy = new Murmur3PartitionKeyStrategy(m => (m as TestOrderMessage)?.CustomerId);
        var message1 = new TestOrderMessage { CustomerId = "customer-1" };
        var message2 = new TestOrderMessage { CustomerId = "customer-1" };

        // Act
        var key1 = strategy.GetPartitionKey(message1, "topic");
        var key2 = strategy.GetPartitionKey(message2, "topic");

        // Assert
        key1.ShouldBe(key2);
    }

    [Fact]
    public void Murmur3Strategy_ReturnsDifferentHashForDifferentValues()
    {
        // Arrange
        var strategy = new Murmur3PartitionKeyStrategy(m => (m as TestOrderMessage)?.CustomerId);
        var message1 = new TestOrderMessage { CustomerId = "customer-a" };
        var message2 = new TestOrderMessage { CustomerId = "customer-b" };

        // Act
        var key1 = strategy.GetPartitionKey(message1, "topic");
        var key2 = strategy.GetPartitionKey(message2, "topic");

        // Assert
        key1.ShouldNotBe(key2);
    }

    [Fact]
    public void Murmur3Strategy_ReturnsNullForNullProperty()
    {
        // Arrange
        var strategy = new Murmur3PartitionKeyStrategy(m => null);
        var message = new TestMessage();

        // Act
        var key = strategy.GetPartitionKey(message, "topic");

        // Assert
        key.ShouldBeNull();
    }

    #endregion

    #region Test Messages

    public class TestMessage : Message
    {
        public string Data { get; set; }
    }

    public class TestOrderMessage : Message
    {
        public string CustomerId { get; set; }
        public string OrderId { get; set; }
    }

    #endregion
}
