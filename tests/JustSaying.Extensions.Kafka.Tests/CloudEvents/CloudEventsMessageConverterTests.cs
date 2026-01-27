using System.Text.Json;
using JustSaying.Extensions.Kafka.CloudEvents;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.CloudEvents;

public class CloudEventsMessageConverterTests
{
    private readonly CloudEventsMessageConverter _converter;
    private readonly IMessageBodySerializationFactory _serializationFactory;

    public CloudEventsMessageConverterTests()
    {
        _serializationFactory = new SystemTextJsonSerializationFactory(new JsonSerializerOptions());
        _converter = new CloudEventsMessageConverter(_serializationFactory, "urn:test:source");
    }

    [Fact]
    public void ToCloudEvent_ShouldConvertMessageToCloudEvent()
    {
        // Arrange
        var message = new TestMessage
        {
            Id = Guid.NewGuid(),
            TimeStamp = DateTime.UtcNow,
            Content = "Test content",
            RaisingComponent = "TestComponent",
            Tenant = "TestTenant"
        };

        // Act
        var cloudEvent = _converter.ToCloudEvent(message);

        // Assert
        cloudEvent.ShouldNotBeNull();
        cloudEvent.Id.ShouldBe(message.Id.ToString());
        cloudEvent.Type.ShouldBe(typeof(TestMessage).FullName);
        cloudEvent.Source.ToString().ShouldBe("urn:test:source");
        cloudEvent.Time.ShouldNotBeNull();
        cloudEvent.DataContentType.ShouldBe("application/json");
        cloudEvent["raisingcomponent"].ShouldBe("TestComponent");
        cloudEvent["tenant"].ShouldBe("TestTenant");
    }

    [Fact]
    public void FromCloudEvent_ShouldConvertCloudEventToMessage()
    {
        // Arrange
        var originalMessage = new TestMessage
        {
            Id = Guid.NewGuid(),
            TimeStamp = DateTime.UtcNow,
            Content = "Test content",
            RaisingComponent = "TestComponent",
            Tenant = "TestTenant"
        };

        var cloudEvent = _converter.ToCloudEvent(originalMessage);

        // Act
        var restoredMessage = _converter.FromCloudEvent<TestMessage>(cloudEvent);

        // Assert
        restoredMessage.ShouldNotBeNull();
        restoredMessage.ShouldBeOfType<TestMessage>();
        restoredMessage.Id.ShouldBe(originalMessage.Id);
        restoredMessage.RaisingComponent.ShouldBe(originalMessage.RaisingComponent);
        restoredMessage.Tenant.ShouldBe(originalMessage.Tenant);
        restoredMessage.Content.ShouldBe(originalMessage.Content);
    }

    [Fact]
    public void SerializeAndDeserialize_ShouldRoundTripCloudEvent()
    {
        // Arrange
        var message = new TestMessage
        {
            Id = Guid.NewGuid(),
            TimeStamp = DateTime.UtcNow,
            Content = "Test content"
        };

        var cloudEvent = _converter.ToCloudEvent(message);

        // Act
        var serialized = _converter.Serialize(cloudEvent);
        var deserialized = _converter.Deserialize(serialized);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(cloudEvent.Id);
        deserialized.Type.ShouldBe(cloudEvent.Type);
        deserialized.Source.ShouldBe(cloudEvent.Source);
    }

    [Fact]
    public void ToCloudEvent_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _converter.ToCloudEvent<TestMessage>(null));
    }

    [Fact]
    public void FromCloudEvent_WithNullCloudEvent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _converter.FromCloudEvent<TestMessage>(null));
    }

    public class TestMessage : Message
    {
        public string Content { get; set; }
    }
}
