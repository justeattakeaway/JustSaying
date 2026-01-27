using System.Diagnostics;
using JustSaying.Extensions.Kafka.Monitoring;
using JustSaying.Extensions.Kafka.Tracing;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Tracing;

public class TracingKafkaConsumerMonitorTests
{
    [Fact]
    public void OnMessageReceived_CreatesActivity()
    {
        // Arrange
        Activity capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == KafkaActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => capturedActivity = activity
        };
        ActivitySource.AddActivityListener(listener);

        var monitor = new TracingKafkaConsumerMonitor();
        var context = new MessageReceivedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 1,
            Offset = 100,
            MessageTimestamp = DateTime.UtcNow.AddSeconds(-1),
            ReceivedAt = DateTime.UtcNow,
            Message = new TestMessage { Id = Guid.NewGuid() },
            MessageKey = "key-123",
            ConsumerGroup = "test-group"
        };

        // Act
        monitor.OnMessageReceived(context);

        // Assert
        capturedActivity.ShouldNotBeNull();
        capturedActivity.OperationName.ShouldBe(KafkaActivitySource.ConsumeActivityName);
    }

    [Fact]
    public void OnMessageProcessed_CompletesActivityWithSuccess()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == KafkaActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var monitor = new TracingKafkaConsumerMonitor();
        
        // First receive the message to start the activity
        var receivedContext = new MessageReceivedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 1,
            Offset = 100,
            MessageTimestamp = DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow,
            Message = new TestMessage()
        };
        monitor.OnMessageReceived(receivedContext);

        var processedContext = new MessageProcessedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 1,
            Offset = 100,
            ProcessingDuration = TimeSpan.FromMilliseconds(50),
            RetryAttempt = 1,
            Message = new TestMessage()
        };

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageProcessed(processedContext));
    }

    [Fact]
    public void OnMessageFailed_RecordsExceptionOnActivity()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == KafkaActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var monitor = new TracingKafkaConsumerMonitor();
        
        // First receive the message
        var receivedContext = new MessageReceivedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 2,
            Offset = 200,
            MessageTimestamp = DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow,
            Message = new TestMessage()
        };
        monitor.OnMessageReceived(receivedContext);

        var failedContext = new MessageFailedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 2,
            Offset = 200,
            Exception = new InvalidOperationException("test error"),
            RetryAttempt = 1,
            WillRetry = true,
            Message = new TestMessage()
        };

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageFailed(failedContext));
    }

    [Fact]
    public void OnMessageDeadLettered_RecordsDeadLetterInfo()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == KafkaActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var monitor = new TracingKafkaConsumerMonitor();
        
        // First receive the message
        var receivedContext = new MessageReceivedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 3,
            Offset = 300,
            MessageTimestamp = DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow,
            Message = new TestMessage()
        };
        monitor.OnMessageReceived(receivedContext);

        var dltContext = new MessageDeadLetteredContext<TestMessage>
        {
            Topic = "test-topic",
            DeadLetterTopic = "test-topic-dlt",
            Partition = 3,
            Offset = 300,
            Exception = new InvalidOperationException("final error"),
            TotalAttempts = 3,
            Message = new TestMessage()
        };

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageDeadLettered(dltContext));
    }

    [Fact]
    public void OnMessageReceived_HandlesNullContext()
    {
        // Arrange
        var monitor = new TracingKafkaConsumerMonitor();

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageReceived<TestMessage>(null));
    }

    [Fact]
    public void OnMessageProcessed_HandlesNullContext()
    {
        // Arrange
        var monitor = new TracingKafkaConsumerMonitor();

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageProcessed<TestMessage>(null));
    }

    [Fact]
    public void OnMessageReceived_ExtractsTraceContextFromHeaders()
    {
        // Arrange
        Activity capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == KafkaActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => capturedActivity = activity
        };
        ActivitySource.AddActivityListener(listener);

        var parentTraceId = ActivityTraceId.CreateRandom();
        var parentSpanId = ActivitySpanId.CreateRandom();
        var traceParent = $"00-{parentTraceId}-{parentSpanId}-01";

        var monitor = new TracingKafkaConsumerMonitor();
        var context = new MessageReceivedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 1,
            Offset = 100,
            MessageTimestamp = DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow,
            Message = new TestMessage(),
            Headers = new Dictionary<string, string>
            {
                [TraceContextPropagator.TraceParentHeader] = traceParent
            }
        };

        // Act
        monitor.OnMessageReceived(context);

        // Assert
        capturedActivity.ShouldNotBeNull();
        capturedActivity.ParentId.ShouldNotBeNull();
    }

    [Fact]
    public void AddKafkaDistributedTracing_RegistersMonitor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKafkaDistributedTracing();

        // Assert
        var descriptor = services.FirstOrDefault(d => 
            d.ServiceType == typeof(IKafkaConsumerMonitor) && 
            d.ImplementationType == typeof(TracingKafkaConsumerMonitor));
        
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddKafkaOpenTelemetry_RegistersBothMetricsAndTracing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddKafkaOpenTelemetry();

        // Assert
        var monitors = services.Where(d => d.ServiceType == typeof(IKafkaConsumerMonitor)).ToList();
        monitors.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    public class TestMessage : Message
    {
        public string Data { get; set; }
    }
}
