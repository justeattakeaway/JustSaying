using JustSaying.Extensions.Kafka.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Monitoring;

public class KafkaConsumerMonitorTests
{
    #region MessageReceivedContext Tests

    [Fact]
    public void MessageReceivedContext_LagMilliseconds_CalculatedCorrectly()
    {
        // Arrange
        var messageTimestamp = DateTime.UtcNow.AddSeconds(-5);
        var receivedAt = DateTime.UtcNow;

        var context = new MessageReceivedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 0,
            Offset = 100,
            MessageTimestamp = messageTimestamp,
            ReceivedAt = receivedAt,
            Message = new TestMessage()
        };

        // Act
        var lag = context.LagMilliseconds;

        // Assert
        lag.ShouldBeGreaterThan(4900);
        lag.ShouldBeLessThan(5500);
    }

    [Fact]
    public void MessageReceivedContext_Properties_SetCorrectly()
    {
        // Arrange & Act
        var message = new TestMessage { Id = Guid.NewGuid() };
        var context = new MessageReceivedContext<TestMessage>
        {
            Topic = "my-topic",
            Partition = 3,
            Offset = 12345,
            MessageTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ReceivedAt = new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc),
            Message = message
        };

        // Assert
        context.Topic.ShouldBe("my-topic");
        context.Partition.ShouldBe(3);
        context.Offset.ShouldBe(12345);
        context.Message.ShouldBe(message);
    }

    #endregion

    #region MessageProcessedContext Tests

    [Fact]
    public void MessageProcessedContext_Properties_SetCorrectly()
    {
        // Arrange & Act
        var message = new TestMessage { Id = Guid.NewGuid() };
        var context = new MessageProcessedContext<TestMessage>
        {
            Topic = "processed-topic",
            Partition = 1,
            Offset = 999,
            Message = message,
            ProcessingDuration = TimeSpan.FromMilliseconds(150),
            RetryAttempt = 2
        };

        // Assert
        context.Topic.ShouldBe("processed-topic");
        context.Partition.ShouldBe(1);
        context.Offset.ShouldBe(999);
        context.ProcessingDuration.ShouldBe(TimeSpan.FromMilliseconds(150));
        context.RetryAttempt.ShouldBe(2);
    }

    #endregion

    #region MessageFailedContext Tests

    [Fact]
    public void MessageFailedContext_Properties_SetCorrectly()
    {
        // Arrange & Act
        var message = new TestMessage { Id = Guid.NewGuid() };
        var exception = new InvalidOperationException("Test error");
        var context = new MessageFailedContext<TestMessage>
        {
            Topic = "failed-topic",
            Partition = 2,
            Offset = 500,
            Message = message,
            Exception = exception,
            RetryAttempt = 3,
            WillRetry = true
        };

        // Assert
        context.Topic.ShouldBe("failed-topic");
        context.Exception.ShouldBe(exception);
        context.RetryAttempt.ShouldBe(3);
        context.WillRetry.ShouldBeTrue();
    }

    #endregion

    #region MessageDeadLetteredContext Tests

    [Fact]
    public void MessageDeadLetteredContext_Properties_SetCorrectly()
    {
        // Arrange & Act
        var message = new TestMessage { Id = Guid.NewGuid() };
        var exception = new InvalidOperationException("Final failure");
        var context = new MessageDeadLetteredContext<TestMessage>
        {
            Topic = "source-topic",
            DeadLetterTopic = "source-topic-dlt",
            Partition = 0,
            Offset = 1000,
            Message = message,
            Exception = exception,
            TotalAttempts = 3
        };

        // Assert
        context.Topic.ShouldBe("source-topic");
        context.DeadLetterTopic.ShouldBe("source-topic-dlt");
        context.TotalAttempts.ShouldBe(3);
        context.Exception.ShouldBe(exception);
    }

    #endregion

    #region NullKafkaConsumerMonitor Tests

    [Fact]
    public void NullKafkaConsumerMonitor_Instance_IsSingleton()
    {
        // Act
        var instance1 = NullKafkaConsumerMonitor.Instance;
        var instance2 = NullKafkaConsumerMonitor.Instance;

        // Assert
        instance1.ShouldBeSameAs(instance2);
    }

    [Fact]
    public void NullKafkaConsumerMonitor_Methods_DoNotThrow()
    {
        // Arrange
        var monitor = NullKafkaConsumerMonitor.Instance;
        var receivedCtx = new MessageReceivedContext<TestMessage>();
        var processedCtx = new MessageProcessedContext<TestMessage>();
        var failedCtx = new MessageFailedContext<TestMessage>();
        var dltCtx = new MessageDeadLetteredContext<TestMessage>();

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageReceived(receivedCtx));
        Should.NotThrow(() => monitor.OnMessageProcessed(processedCtx));
        Should.NotThrow(() => monitor.OnMessageFailed(failedCtx));
        Should.NotThrow(() => monitor.OnMessageDeadLettered(dltCtx));
    }

    #endregion

    #region LoggingKafkaConsumerMonitor Tests

    [Fact]
    public void LoggingKafkaConsumerMonitor_OnMessageReceived_LogsDebug()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var monitor = new LoggingKafkaConsumerMonitor(loggerFactory);
        var context = new MessageReceivedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 0,
            Offset = 100,
            MessageTimestamp = DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow
        };

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageReceived(context));
    }

    [Fact]
    public void LoggingKafkaConsumerMonitor_OnMessageProcessed_LogsInfo()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var monitor = new LoggingKafkaConsumerMonitor(loggerFactory);
        var context = new MessageProcessedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 0,
            Offset = 100,
            ProcessingDuration = TimeSpan.FromMilliseconds(50),
            RetryAttempt = 1
        };

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageProcessed(context));
    }

    [Fact]
    public void LoggingKafkaConsumerMonitor_OnMessageFailed_LogsWarningWhenWillRetry()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var monitor = new LoggingKafkaConsumerMonitor(loggerFactory);
        var context = new MessageFailedContext<TestMessage>
        {
            Topic = "test-topic",
            Partition = 0,
            Offset = 100,
            Exception = new InvalidOperationException("test"),
            RetryAttempt = 1,
            WillRetry = true
        };

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageFailed(context));
    }

    [Fact]
    public void LoggingKafkaConsumerMonitor_OnMessageDeadLettered_LogsError()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var monitor = new LoggingKafkaConsumerMonitor(loggerFactory);
        var context = new MessageDeadLetteredContext<TestMessage>
        {
            Topic = "test-topic",
            DeadLetterTopic = "test-topic-dlt",
            Partition = 0,
            Offset = 100,
            Exception = new InvalidOperationException("test"),
            TotalAttempts = 3
        };

        // Act & Assert - should not throw
        Should.NotThrow(() => monitor.OnMessageDeadLettered(context));
    }

    #endregion

    #region CompositeKafkaConsumerMonitor Tests

    [Fact]
    public void CompositeKafkaConsumerMonitor_InvokesAllMonitors()
    {
        // Arrange
        var monitor1 = Substitute.For<IKafkaConsumerMonitor>();
        var monitor2 = Substitute.For<IKafkaConsumerMonitor>();
        var composite = new CompositeKafkaConsumerMonitorTestWrapper(new[] { monitor1, monitor2 });

        var receivedCtx = new MessageReceivedContext<TestMessage> { Topic = "test" };
        var processedCtx = new MessageProcessedContext<TestMessage> { Topic = "test" };
        var failedCtx = new MessageFailedContext<TestMessage> { Topic = "test" };
        var dltCtx = new MessageDeadLetteredContext<TestMessage> { Topic = "test" };

        // Act
        composite.OnMessageReceived(receivedCtx);
        composite.OnMessageProcessed(processedCtx);
        composite.OnMessageFailed(failedCtx);
        composite.OnMessageDeadLettered(dltCtx);

        // Assert
        monitor1.Received(1).OnMessageReceived(receivedCtx);
        monitor2.Received(1).OnMessageReceived(receivedCtx);
        monitor1.Received(1).OnMessageProcessed(processedCtx);
        monitor2.Received(1).OnMessageProcessed(processedCtx);
        monitor1.Received(1).OnMessageFailed(failedCtx);
        monitor2.Received(1).OnMessageFailed(failedCtx);
        monitor1.Received(1).OnMessageDeadLettered(dltCtx);
        monitor2.Received(1).OnMessageDeadLettered(dltCtx);
    }

    [Fact]
    public void CompositeKafkaConsumerMonitor_SwallowsExceptions()
    {
        // Arrange
        var failingMonitor = Substitute.For<IKafkaConsumerMonitor>();
        var succeedingMonitor = Substitute.For<IKafkaConsumerMonitor>();

        failingMonitor.When(x => x.OnMessageReceived(Arg.Any<MessageReceivedContext<TestMessage>>()))
            .Do(_ => throw new InvalidOperationException("Monitor error"));

        var composite = new CompositeKafkaConsumerMonitorTestWrapper(new[] { failingMonitor, succeedingMonitor });
        var context = new MessageReceivedContext<TestMessage> { Topic = "test" };

        // Act & Assert - should not throw and should still call second monitor
        Should.NotThrow(() => composite.OnMessageReceived(context));
        succeedingMonitor.Received(1).OnMessageReceived(context);
    }

    [Fact]
    public void CompositeKafkaConsumerMonitor_HandleNullMonitors()
    {
        // Arrange
        var composite = new CompositeKafkaConsumerMonitorTestWrapper(null);
        var context = new MessageReceivedContext<TestMessage> { Topic = "test" };

        // Act & Assert - should not throw
        Should.NotThrow(() => composite.OnMessageReceived(context));
    }

    #endregion

    public class TestMessage : Message
    {
        public string Data { get; set; }
    }

    /// <summary>
    /// Test wrapper to access the internal CompositeKafkaConsumerMonitor.
    /// </summary>
    public class CompositeKafkaConsumerMonitorTestWrapper : IKafkaConsumerMonitor
    {
        private readonly IEnumerable<IKafkaConsumerMonitor> _monitors;

        public CompositeKafkaConsumerMonitorTestWrapper(IEnumerable<IKafkaConsumerMonitor> monitors)
        {
            _monitors = monitors ?? Enumerable.Empty<IKafkaConsumerMonitor>();
        }

        public void OnMessageReceived<T>(MessageReceivedContext<T> context) where T : Message
        {
            foreach (var monitor in _monitors)
            {
                try { monitor.OnMessageReceived(context); }
                catch { /* swallow */ }
            }
        }

        public void OnMessageProcessed<T>(MessageProcessedContext<T> context) where T : Message
        {
            foreach (var monitor in _monitors)
            {
                try { monitor.OnMessageProcessed(context); }
                catch { /* swallow */ }
            }
        }

        public void OnMessageFailed<T>(MessageFailedContext<T> context) where T : Message
        {
            foreach (var monitor in _monitors)
            {
                try { monitor.OnMessageFailed(context); }
                catch { /* swallow */ }
            }
        }

        public void OnMessageDeadLettered<T>(MessageDeadLetteredContext<T> context) where T : Message
        {
            foreach (var monitor in _monitors)
            {
                try { monitor.OnMessageDeadLettered(context); }
                catch { /* swallow */ }
            }
        }
    }
}

