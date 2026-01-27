using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Fluent;
using JustSaying.Models;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Configuration;

public class NumberOfConsumersTests
{
    #region KafkaConfiguration Tests

    [Fact]
    public void KafkaConfiguration_NumberOfConsumers_DefaultsToOne()
    {
        // Arrange & Act
        var config = new KafkaConfiguration();

        // Assert
        config.NumberOfConsumers.ShouldBe(1u);
    }

    [Fact]
    public void KafkaConfiguration_NumberOfConsumers_CanBeSet()
    {
        // Arrange
        var config = new KafkaConfiguration();

        // Act
        config.NumberOfConsumers = 4;

        // Assert
        config.NumberOfConsumers.ShouldBe(4u);
    }

    #endregion

    #region KafkaSubscriptionBuilder Tests

    [Fact]
    public void WithNumberOfConsumers_SetsNumberOfConsumers()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");

        // Act
        builder.WithNumberOfConsumers(3);

        // Assert
        builder.GetNumberOfConsumers().ShouldBe(3u);
    }

    [Fact]
    public void WithNumberOfConsumers_ThrowsWhenZero()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.WithNumberOfConsumers(0));
    }

    [Fact]
    public void WithNumberOfConsumers_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");

        // Act
        var result = builder.WithNumberOfConsumers(2);

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void WithNumberOfConsumers_WorksWithOtherConfiguration()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");

        // Act
        builder
            .WithBootstrapServers("localhost:9092")
            .WithGroupId("test-group")
            .WithNumberOfConsumers(4)
            .WithDeadLetterTopic("test-topic-dlt");

        // Assert
        builder.GetNumberOfConsumers().ShouldBe(4u);
        var config = builder.GetConfiguration();
        config.BootstrapServers.ShouldBe("localhost:9092");
        config.GroupId.ShouldBe("test-group");
        config.DeadLetterTopic.ShouldBe("test-topic-dlt");
    }

    [Fact]
    public void GetConfiguration_ReturnsConfiguration()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");
        builder.WithBootstrapServers("localhost:9092");

        // Act
        var config = builder.GetConfiguration();

        // Assert
        config.ShouldNotBeNull();
        config.BootstrapServers.ShouldBe("localhost:9092");
    }

    #endregion

    public class TestMessage : Message
    {
        public string Data { get; set; }
    }
}

