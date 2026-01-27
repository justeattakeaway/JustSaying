using System.Diagnostics;
using JustSaying.Extensions.Kafka.Tracing;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Tracing;

public class KafkaActivitySourceTests
{
    [Fact]
    public void SourceName_IsCorrect()
    {
        KafkaActivitySource.SourceName.ShouldBe("JustSaying.Kafka");
    }

    [Fact]
    public void SourceVersion_IsCorrect()
    {
        KafkaActivitySource.SourceVersion.ShouldBe("1.0.0");
    }

    [Fact]
    public void Source_IsNotNull()
    {
        KafkaActivitySource.Source.ShouldNotBeNull();
        KafkaActivitySource.Source.Name.ShouldBe("JustSaying.Kafka");
    }

    [Fact]
    public void StartProduceActivity_ReturnsActivityWithCorrectTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = KafkaActivitySource.StartProduceActivity(
            "test-topic",
            "msg-123",
            "key-456");

        // Assert
        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe(KafkaActivitySource.ProduceActivityName);
        activity.Kind.ShouldBe(ActivityKind.Producer);
        
        var tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value);
        tags[KafkaActivitySource.MessagingSystemTag].ShouldBe("kafka");
        tags[KafkaActivitySource.MessagingDestinationTag].ShouldBe("test-topic");
        tags[KafkaActivitySource.MessagingOperationTag].ShouldBe("publish");
        tags[KafkaActivitySource.MessagingMessageIdTag].ShouldBe("msg-123");
        tags[KafkaActivitySource.MessagingKafkaMessageKeyTag].ShouldBe("key-456");
    }

    [Fact]
    public void StartConsumeActivity_ReturnsActivityWithCorrectTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = KafkaActivitySource.StartConsumeActivity(
            "test-topic",
            partition: 2,
            offset: 12345,
            consumerGroup: "my-group",
            messageId: "msg-789",
            messageKey: "key-abc");

        // Assert
        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe(KafkaActivitySource.ConsumeActivityName);
        activity.Kind.ShouldBe(ActivityKind.Consumer);
        
        var tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value);
        tags[KafkaActivitySource.MessagingSystemTag].ShouldBe("kafka");
        tags[KafkaActivitySource.MessagingDestinationTag].ShouldBe("test-topic");
        tags[KafkaActivitySource.MessagingOperationTag].ShouldBe("receive");
        tags[KafkaActivitySource.MessagingKafkaPartitionTag].ShouldBe("2");
        tags[KafkaActivitySource.MessagingKafkaOffsetTag].ShouldBe("12345");
        tags[KafkaActivitySource.MessagingKafkaConsumerGroupTag].ShouldBe("my-group");
    }

    [Fact]
    public void StartProcessActivity_ReturnsActivityWithCorrectTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = KafkaActivitySource.StartProcessActivity(
            "test-topic",
            "msg-456",
            partition: 1,
            offset: 999);

        // Assert
        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe(KafkaActivitySource.ProcessActivityName);
        
        var tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value);
        tags[KafkaActivitySource.MessagingOperationTag].ShouldBe("process");
    }

    [Fact]
    public void RecordException_SetsErrorStatus()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = KafkaActivitySource.StartProduceActivity("topic", "id");
        var exception = new InvalidOperationException("Test error");

        // Act
        KafkaActivitySource.RecordException(activity, exception);

        // Assert
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Test error");
    }

    [Fact]
    public void SetSuccess_SetsOkStatus()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = KafkaActivitySource.StartProduceActivity("topic", "id");

        // Act
        KafkaActivitySource.SetSuccess(activity);

        // Assert
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordException_HandlesNullActivity()
    {
        // Act & Assert - should not throw
        Should.NotThrow(() => KafkaActivitySource.RecordException(null, new Exception()));
    }

    [Fact]
    public void SetSuccess_HandlesNullActivity()
    {
        // Act & Assert - should not throw
        Should.NotThrow(() => KafkaActivitySource.SetSuccess(null));
    }
}
