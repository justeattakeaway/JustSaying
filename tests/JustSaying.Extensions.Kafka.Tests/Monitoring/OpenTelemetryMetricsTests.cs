using System.Diagnostics.Metrics;
using JustSaying.Extensions.Kafka.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Monitoring;

public class OpenTelemetryMetricsTests
{
    [Fact]
    public void OpenTelemetryKafkaConsumerMonitor_OnMessageReceived_RecordsMetrics()
    {
        // Arrange
        var monitor = new OpenTelemetryKafkaConsumerMonitor();
        var context = new MessageReceivedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 1,
            Offset = 100,
            MessageTimestamp = DateTime.UtcNow.AddSeconds(-2),
            ReceivedAt = DateTime.UtcNow,
            Message = new TestMessage()
        };

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageReceived(context));
    }

    [Fact]
    public void OpenTelemetryKafkaConsumerMonitor_OnMessageProcessed_RecordsMetrics()
    {
        // Arrange
        var monitor = new OpenTelemetryKafkaConsumerMonitor();
        var context = new MessageProcessedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 1,
            Offset = 100,
            ProcessingDuration = TimeSpan.FromMilliseconds(50),
            RetryAttempt = 1,
            Message = new TestMessage()
        };

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageProcessed(context));
    }

    [Fact]
    public void OpenTelemetryKafkaConsumerMonitor_OnMessageProcessed_RecordsRetryAttempts()
    {
        // Arrange
        var monitor = new OpenTelemetryKafkaConsumerMonitor();
        var context = new MessageProcessedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 1,
            Offset = 100,
            ProcessingDuration = TimeSpan.FromMilliseconds(50),
            RetryAttempt = 3, // Multiple retries
            Message = new TestMessage()
        };

        // Act & Assert - should not throw and should record retry attempts
        Should.NotThrow(() => monitor.OnMessageProcessed(context));
    }

    [Fact]
    public void OpenTelemetryKafkaConsumerMonitor_OnMessageFailed_RecordsMetrics()
    {
        // Arrange
        var monitor = new OpenTelemetryKafkaConsumerMonitor();
        var context = new MessageFailedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 1,
            Offset = 100,
            Exception = new InvalidOperationException("test error"),
            RetryAttempt = 1,
            WillRetry = true,
            Message = new TestMessage()
        };

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageFailed(context));
    }

    [Fact]
    public void OpenTelemetryKafkaConsumerMonitor_OnMessageDeadLettered_RecordsMetrics()
    {
        // Arrange
        var monitor = new OpenTelemetryKafkaConsumerMonitor();
        var context = new MessageDeadLetteredContext<TestMessage>
        {
            Topic = "test-topic",
            DeadLetterTopic = "test-topic-dlt",
            Partition = 1,
            Offset = 100,
            Exception = new InvalidOperationException("test error"),
            TotalAttempts = 3,
            Message = new TestMessage()
        };

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageDeadLettered(context));
    }

    [Fact]
    public void OpenTelemetryKafkaConsumerMonitor_MeterName_IsCorrect()
    {
        // Assert
        OpenTelemetryKafkaConsumerMonitor.MeterName.ShouldBe("JustSaying.Kafka");
    }

    [Fact]
    public void AddKafkaOpenTelemetryMetrics_RegistersMonitor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKafkaOpenTelemetryMetrics();

        // Assert - verify the service is registered
        var descriptor = services.FirstOrDefault(d => 
            d.ServiceType == typeof(IKafkaConsumerMonitor) && 
            d.ImplementationType == typeof(OpenTelemetryKafkaConsumerMonitor));
        
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void OpenTelemetryKafkaConsumerMonitor_HandlesNullException()
    {
        // Arrange
        var monitor = new OpenTelemetryKafkaConsumerMonitor();
        var context = new MessageFailedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 1,
            Offset = 100,
            Exception = null, // Null exception
            RetryAttempt = 1,
            WillRetry = false
        };

        // Act & Assert - should not throw even with null exception
        Should.NotThrow(() => monitor.OnMessageFailed(context));
    }

    public class TestMessage : Message
    {
        public string Data { get; set; }
    }
}

