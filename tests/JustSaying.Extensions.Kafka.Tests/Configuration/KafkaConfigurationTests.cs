using JustSaying.Extensions.Kafka.Configuration;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Configuration;

public class KafkaConfigurationTests
{
    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var config = new KafkaConfiguration
        {
            BootstrapServers = "localhost:9092"
        };

        // Act & Assert
        Should.NotThrow(() => config.Validate());
    }

    [Fact]
    public void Validate_WithNullBootstrapServers_ShouldThrow()
    {
        // Arrange
        var config = new KafkaConfiguration
        {
            BootstrapServers = null
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => config.Validate())
            .Message.ShouldContain("Bootstrap servers must be configured");
    }

    [Fact]
    public void Validate_WithEmptyBootstrapServers_ShouldThrow()
    {
        // Arrange
        var config = new KafkaConfiguration
        {
            BootstrapServers = ""
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void GetProducerConfig_ShouldUseBootstrapServers()
    {
        // Arrange
        var config = new KafkaConfiguration
        {
            BootstrapServers = "localhost:9092"
        };

        // Act
        var producerConfig = config.GetProducerConfig();

        // Assert
        producerConfig.BootstrapServers.ShouldBe("localhost:9092");
    }

    [Fact]
    public void GetConsumerConfig_ShouldUseBootstrapServersAndGroupId()
    {
        // Arrange
        var config = new KafkaConfiguration
        {
            BootstrapServers = "localhost:9092",
            GroupId = "test-group"
        };

        // Act
        var consumerConfig = config.GetConsumerConfig();

        // Assert
        consumerConfig.BootstrapServers.ShouldBe("localhost:9092");
        consumerConfig.GroupId.ShouldBe("test-group");
        consumerConfig.EnableAutoCommit.ShouldBe(false);
    }

    [Fact]
    public void EnableCloudEvents_DefaultsToTrue()
    {
        // Arrange & Act
        var config = new KafkaConfiguration();

        // Assert
        config.EnableCloudEvents.ShouldBeTrue();
    }

    [Fact]
    public void CloudEventsSource_HasDefaultValue()
    {
        // Arrange & Act
        var config = new KafkaConfiguration();

        // Assert
        config.CloudEventsSource.ShouldBe("urn:justsaying");
    }

    [Fact]
    public void DeadLetterTopic_DefaultsToNull()
    {
        // Arrange & Act
        var config = new KafkaConfiguration();

        // Assert
        config.DeadLetterTopic.ShouldBeNull();
    }

    [Fact]
    public void FailureTopic_DefaultsToNull()
    {
        // Arrange & Act
        var config = new KafkaConfiguration();

        // Assert
        config.FailureTopic.ShouldBeNull();
    }

    [Fact]
    public void DelayInMs_DefaultsToZero()
    {
        // Arrange & Act
        var config = new KafkaConfiguration();

        // Assert
        config.DelayInMs.ShouldBe(0u);
    }

    [Fact]
    public void Retry_HasDefaultConfiguration()
    {
        // Arrange & Act
        var config = new KafkaConfiguration();

        // Assert
        config.Retry.ShouldNotBeNull();
        config.Retry.Mode.ShouldBe(RetryMode.InProcess);
        config.Retry.MaxRetryAttempts.ShouldBe(3);
    }

    [Fact]
    public void FailureHandlerFactory_DefaultsToNull()
    {
        // Arrange & Act
        var config = new KafkaConfiguration();

        // Assert
        config.FailureHandlerFactory.ShouldBeNull();
    }

    [Fact]
    public void DeadLetterTopic_CanBeSet()
    {
        // Arrange
        var config = new KafkaConfiguration();

        // Act
        config.DeadLetterTopic = "my-topic.dlt";

        // Assert
        config.DeadLetterTopic.ShouldBe("my-topic.dlt");
    }

    [Fact]
    public void FailureTopic_CanBeSet()
    {
        // Arrange
        var config = new KafkaConfiguration();

        // Act
        config.FailureTopic = "my-topic.retry-1";

        // Assert
        config.FailureTopic.ShouldBe("my-topic.retry-1");
    }

    [Fact]
    public void DelayInMs_CanBeSet()
    {
        // Arrange
        var config = new KafkaConfiguration();

        // Act
        config.DelayInMs = 30000;

        // Assert
        config.DelayInMs.ShouldBe(30000u);
    }
}
