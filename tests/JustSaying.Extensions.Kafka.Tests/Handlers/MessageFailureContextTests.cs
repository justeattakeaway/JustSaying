using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Handlers;
using JustSaying.Models;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Handlers;

public class MessageFailureContextTests
{
    [Fact]
    public void MessageFailureContext_CanBeCreatedWithAllProperties()
    {
        // Arrange
        var kafkaResult = new ConsumeResult<string, byte[]>
        {
            Topic = "test-topic",
            Partition = new Partition(1),
            Offset = new Offset(100),
            Message = new Message<string, byte[]>
            {
                Key = "test-key",
                Value = new byte[] { 1, 2, 3 }
            }
        };
        var message = new TestMessage { Id = Guid.NewGuid() };

        // Act
        var context = new MessageFailureContext<TestMessage>
        {
            KafkaResult = kafkaResult,
            Message = message,
            Topic = "test-topic",
            Partition = 1,
            Offset = 100,
            RetryAttempt = 3,
            RetriesExhausted = true
        };

        // Assert
        context.KafkaResult.ShouldBe(kafkaResult);
        context.Message.ShouldBe(message);
        context.Topic.ShouldBe("test-topic");
        context.Partition.ShouldBe(1);
        context.Offset.ShouldBe(100);
        context.RetryAttempt.ShouldBe(3);
        context.RetriesExhausted.ShouldBeTrue();
    }

    [Fact]
    public void MessageFailureContext_DefaultValues()
    {
        // Arrange & Act
        var context = new MessageFailureContext<TestMessage>();

        // Assert
        context.KafkaResult.ShouldBeNull();
        context.Message.ShouldBeNull();
        context.Topic.ShouldBeNull();
        context.Partition.ShouldBe(0);
        context.Offset.ShouldBe(0);
        context.RetryAttempt.ShouldBe(0);
        context.RetriesExhausted.ShouldBeFalse();
    }

    private class TestMessage : Message
    {
    }
}

