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
}
