using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Fluent;
using JustSaying.Extensions.Kafka.Handlers;
using JustSaying.Models;
using NSubstitute;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Fluent;

public class KafkaSubscriptionBuilderRetryTests
{
    [Fact]
    public void WithDeadLetterTopic_SetsDeadLetterTopic()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");

        // Act
        var result = builder.WithDeadLetterTopic("test-topic.dlt");

        // Assert
        result.ShouldBeSameAs(builder); // Fluent return
        
        // Verify through configuration - we need to use reflection or test indirectly
        var config = GetConfiguration(builder);
        config.DeadLetterTopic.ShouldBe("test-topic.dlt");
    }

    [Fact]
    public void WithInProcessRetry_SetsInProcessRetryMode()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");

        // Act
        builder.WithInProcessRetry(
            maxAttempts: 5,
            initialBackoff: TimeSpan.FromSeconds(10),
            exponentialBackoff: false,
            maxBackoff: TimeSpan.FromMinutes(2));

        // Assert
        var config = GetConfiguration(builder);
        config.Retry.Mode.ShouldBe(RetryMode.InProcess);
        config.Retry.MaxRetryAttempts.ShouldBe(5);
        config.Retry.InitialBackoff.ShouldBe(TimeSpan.FromSeconds(10));
        config.Retry.ExponentialBackoff.ShouldBeFalse();
        config.Retry.MaxBackoff.ShouldBe(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void WithInProcessRetry_WithDefaults_UsesDefaultValues()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");

        // Act
        builder.WithInProcessRetry();

        // Assert
        var config = GetConfiguration(builder);
        config.Retry.Mode.ShouldBe(RetryMode.InProcess);
        config.Retry.MaxRetryAttempts.ShouldBe(3);
        config.Retry.InitialBackoff.ShouldBe(TimeSpan.FromSeconds(5));
        config.Retry.ExponentialBackoff.ShouldBeTrue();
        config.Retry.MaxBackoff.ShouldBe(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void WithTopicChainingRetry_SetsTopicChainingMode()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");

        // Act
        builder.WithTopicChainingRetry("test-topic.retry-1");

        // Assert
        var config = GetConfiguration(builder);
        config.Retry.Mode.ShouldBe(RetryMode.TopicChaining);
        config.FailureTopic.ShouldBe("test-topic.retry-1");
    }

    [Fact]
    public void WithProcessingDelay_SetsDelayInMs()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");

        // Act
        builder.WithProcessingDelay(TimeSpan.FromSeconds(30));

        // Assert
        var config = GetConfiguration(builder);
        config.DelayInMs.ShouldBe(30000u);
    }

    [Fact]
    public void WithNoRetry_SetsMaxRetryAttemptsToZero()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");

        // Act
        builder.WithNoRetry();

        // Assert
        var config = GetConfiguration(builder);
        config.Retry.MaxRetryAttempts.ShouldBe(0);
    }

    [Fact]
    public void WithFailureHandler_SetsCustomHandler()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");
        var customHandler = Substitute.For<IFailureHandler<TestMessage>>();

        // Act
        builder.WithFailureHandler(_ => customHandler);

        // Assert
        var config = GetConfiguration(builder);
        config.FailureHandlerFactory.ShouldNotBeNull();
    }

    [Fact]
    public void FluentChaining_WorksCorrectly()
    {
        // Arrange
        var builder = new KafkaSubscriptionBuilder<TestMessage>("test-topic");

        // Act
        builder
            .WithBootstrapServers("localhost:9092")
            .WithGroupId("test-group")
            .WithDeadLetterTopic("test-topic.dlt")
            .WithInProcessRetry(maxAttempts: 5, initialBackoff: TimeSpan.FromSeconds(2));

        // Assert
        var config = GetConfiguration(builder);
        config.BootstrapServers.ShouldBe("localhost:9092");
        config.GroupId.ShouldBe("test-group");
        config.DeadLetterTopic.ShouldBe("test-topic.dlt");
        config.Retry.MaxRetryAttempts.ShouldBe(5);
        config.Retry.InitialBackoff.ShouldBe(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void TopicChainingSetup_WithRetryTopic_ConfiguresCorrectly()
    {
        // Arrange - Main consumer
        var mainBuilder = new KafkaSubscriptionBuilder<TestMessage>("orders");
        mainBuilder
            .WithBootstrapServers("localhost:9092")
            .WithGroupId("order-processor")
            .WithTopicChainingRetry("orders.retry-1");

        // Arrange - Retry consumer
        var retryBuilder = new KafkaSubscriptionBuilder<TestMessage>("orders.retry-1");
        retryBuilder
            .WithBootstrapServers("localhost:9092")
            .WithGroupId("order-processor-retry")
            .WithProcessingDelay(TimeSpan.FromSeconds(30))
            .WithTopicChainingRetry("orders.dlt");

        // Assert - Main
        var mainConfig = GetConfiguration(mainBuilder);
        mainConfig.Retry.Mode.ShouldBe(RetryMode.TopicChaining);
        mainConfig.FailureTopic.ShouldBe("orders.retry-1");

        // Assert - Retry
        var retryConfig = GetConfiguration(retryBuilder);
        retryConfig.Retry.Mode.ShouldBe(RetryMode.TopicChaining);
        retryConfig.DelayInMs.ShouldBe(30000u);
        retryConfig.FailureTopic.ShouldBe("orders.dlt");
    }

    // Helper method to access configuration via reflection
    private static KafkaConfiguration GetConfiguration(KafkaSubscriptionBuilder<TestMessage> builder)
    {
        var field = typeof(KafkaSubscriptionBuilder<TestMessage>)
            .GetField("_configuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (KafkaConfiguration)field!.GetValue(builder)!;
    }

    // Must be public for NSubstitute proxy generation with strong-named assemblies
    public class TestMessage : Message
    {
    }
}

