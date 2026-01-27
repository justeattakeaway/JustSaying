using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Fluent;
using JustSaying.Extensions.Kafka.Partitioning;
using JustSaying.Models;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Partitioning;

public class PartitioningFluentApiTests
{
    #region Subscription Builder Tests

    [Fact]
    public void WithMessageIdPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("topic");

        // Act
        builder.WithMessageIdPartitioning();
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldNotBeNull();
        config.PartitionKeyStrategy.ShouldBeOfType<MessageIdPartitionKeyStrategy>();
    }

    [Fact]
    public void WithUniqueKeyPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("topic");

        // Act
        builder.WithUniqueKeyPartitioning();
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeOfType<UniqueKeyPartitionKeyStrategy>();
    }

    [Fact]
    public void WithRoundRobinPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("topic");

        // Act
        builder.WithRoundRobinPartitioning();
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeOfType<RoundRobinPartitionKeyStrategy>();
    }

    [Fact]
    public void WithStickyPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("topic");

        // Act
        builder.WithStickyPartitioning(TimeSpan.FromSeconds(30));
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeOfType<StickyPartitionKeyStrategy>();
    }

    [Fact]
    public void WithTimeBasedPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("topic");

        // Act
        builder.WithTimeBasedPartitioning(TimeSpan.FromMinutes(15));
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeOfType<TimeBasedPartitionKeyStrategy>();
    }

    [Fact]
    public void WithConsistentHashPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestOrderMessage>("topic");

        // Act
        builder.WithConsistentHashPartitioning(m => m.CustomerId);
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeOfType<ConsistentHashPartitionKeyStrategy<TestOrderMessage>>();
    }

    [Fact]
    public void WithCustomPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("topic");

        // Act
        builder.WithCustomPartitioning((msg, topic) => $"{topic}:{msg.Id}");
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeOfType<DelegatePartitionKeyStrategy<TestMessage>>();
    }

    [Fact]
    public void WithPartitionKeyStrategy_SetsCustomStrategy()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("topic");
        var customStrategy = new Murmur3PartitionKeyStrategy(m => "custom");

        // Act
        builder.WithPartitionKeyStrategy(customStrategy);
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeSameAs(customStrategy);
    }

    [Fact]
    public void WithPartitionKeyStrategy_ThrowsForNull()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("topic");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.WithPartitionKeyStrategy(null));
    }

    #endregion

    #region Publisher Builder Tests

    [Fact]
    public void PublisherBuilder_WithMessageIdPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaPublisherBuilder<TestMessage>("topic");

        // Act
        builder.WithMessageIdPartitioning();
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeOfType<MessageIdPartitionKeyStrategy>();
    }

    [Fact]
    public void PublisherBuilder_WithRoundRobinPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaPublisherBuilder<TestMessage>("topic");

        // Act
        builder.WithRoundRobinPartitioning();
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeOfType<RoundRobinPartitionKeyStrategy>();
    }

    [Fact]
    public void PublisherBuilder_WithStickyPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaPublisherBuilder<TestMessage>("topic");

        // Act
        builder.WithStickyPartitioning();
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeOfType<StickyPartitionKeyStrategy>();
    }

    [Fact]
    public void PublisherBuilder_WithConsistentHashPartitioning_SetsStrategy()
    {
        // Arrange
        var builder = new KafkaPublisherBuilder<TestOrderMessage>("topic");

        // Act
        builder.WithConsistentHashPartitioning(m => m.OrderId);
        var config = builder.GetConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeOfType<ConsistentHashPartitionKeyStrategy<TestOrderMessage>>();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void KafkaConfiguration_PartitionKeyStrategy_DefaultsToNull()
    {
        // Arrange & Act
        var config = new KafkaConfiguration();

        // Assert
        config.PartitionKeyStrategy.ShouldBeNull();
    }

    [Fact]
    public void KafkaConfiguration_PartitionKeyStrategy_CanBeSet()
    {
        // Arrange
        var config = new KafkaConfiguration();
        var strategy = MessageIdPartitionKeyStrategy.Instance;

        // Act
        config.PartitionKeyStrategy = strategy;

        // Assert
        config.PartitionKeyStrategy.ShouldBeSameAs(strategy);
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
