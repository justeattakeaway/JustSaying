using System.Diagnostics;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Extensions.OpenTelemetry;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace JustSaying.Extensions.OpenTelemetry.Tests;

[Collection("Tracing")]
public class JustSayingBusActivityTests
{
    [Fact]
    public async Task PublishAsync_Creates_Producer_Activity_With_Correct_Tags()
    {
        // Arrange
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedActivities)
            .Build();

        var config = Substitute.For<IMessagingConfig>();
        var publisher = Substitute.For<IMessagePublisher>();
        var monitor = Substitute.For<IMessageMonitor>();
        var serializationFactory = Substitute.For<IMessageBodySerializationFactory>();

        using var bus = new JustSayingBus(config, serializationFactory,
            NullLoggerFactory.Instance, monitor);
        bus.AddMessagePublisher<SimpleMessage>(publisher);

        var message = new SimpleMessage { Id = Guid.NewGuid() };

        // Act
        await bus.PublishAsync(message, CancellationToken.None);
        tracerProvider.ForceFlush();

        // Assert
        var activity = exportedActivities.FirstOrDefault(a => a.OperationName.Contains("publish"));
        activity.ShouldNotBeNull();
        activity.Kind.ShouldBe(ActivityKind.Producer);
        activity.GetTagItem("messaging.operation.name").ShouldBe("publish");
        activity.GetTagItem("messaging.operation.type").ShouldBe("send");
        activity.GetTagItem("messaging.message.id").ShouldBe(message.Id.ToString());
        activity.GetTagItem("messaging.message.type").ShouldBe(typeof(SimpleMessage).FullName);
    }

    [Fact]
    public async Task PublishAsync_Records_Error_On_Activity_When_Publish_Fails()
    {
        // Arrange
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedActivities)
            .Build();

        var config = Substitute.For<IMessagingConfig>();
        config.PublishFailureReAttempts.Returns(1);
        config.PublishFailureBackoff.Returns(TimeSpan.Zero);

        var publisher = Substitute.For<IMessagePublisher>();
        publisher.PublishAsync(Arg.Any<Message>(), Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Publish failed")));

        var monitor = Substitute.For<IMessageMonitor>();
        var serializationFactory = Substitute.For<IMessageBodySerializationFactory>();

        using var bus = new JustSayingBus(config, serializationFactory,
            NullLoggerFactory.Instance, monitor);
        bus.AddMessagePublisher<SimpleMessage>(publisher);

        var message = new SimpleMessage { Id = Guid.NewGuid() };

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            () => bus.PublishAsync(message, CancellationToken.None));
        tracerProvider.ForceFlush();

        // Assert
        var activity = exportedActivities.FirstOrDefault(a => a.OperationName.Contains("publish"));
        activity.ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.Events.ShouldContain(e => e.Name == "exception");
    }

    [Fact]
    public async Task BatchPublishAsync_Creates_Producer_Activity_With_Correct_Tags()
    {
        // Arrange
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedActivities)
            .Build();

        var config = Substitute.For<IMessagingConfig>();
        var publisher = Substitute.For<IMessagePublisher, IMessageBatchPublisher>();
        var monitor = Substitute.For<IMessageMonitor>();
        var serializationFactory = Substitute.For<IMessageBodySerializationFactory>();

        using var bus = new JustSayingBus(config, serializationFactory,
            NullLoggerFactory.Instance, monitor);
        bus.AddMessagePublisher<SimpleMessage>(publisher);

        var messages = new List<Message>
        {
            new SimpleMessage { Id = Guid.NewGuid() },
            new SimpleMessage { Id = Guid.NewGuid() }
        };

        // Act
        await bus.PublishAsync(messages, null, CancellationToken.None);
        tracerProvider.ForceFlush();

        // Assert
        var activity = exportedActivities.FirstOrDefault(a => a.OperationName.Contains("publish"));
        activity.ShouldNotBeNull();
        activity.Kind.ShouldBe(ActivityKind.Producer);
        activity.GetTagItem("messaging.operation.name").ShouldBe("publish");
        activity.GetTagItem("messaging.operation.type").ShouldBe("send");
        activity.GetTagItem("messaging.message.type").ShouldBe(typeof(SimpleMessage).FullName);
        activity.GetTagItem("messaging.batch.message_count").ShouldBe(2);
    }

    [Fact]
    public async Task BatchPublishAsync_Records_Error_On_Activity_When_Publish_Fails()
    {
        // Arrange
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedActivities)
            .Build();

        var config = Substitute.For<IMessagingConfig, IPublishBatchConfiguration>();
        ((IPublishBatchConfiguration)config).PublishFailureReAttempts.Returns(1);
        ((IPublishBatchConfiguration)config).PublishFailureBackoff.Returns(TimeSpan.Zero);
        config.PublishFailureBackoff.Returns(TimeSpan.Zero);

        var publisher = Substitute.For<IMessagePublisher, IMessageBatchPublisher>();
        ((IMessageBatchPublisher)publisher).PublishAsync(
                Arg.Any<IReadOnlyCollection<Message>>(),
                Arg.Any<PublishBatchMetadata>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Batch publish failed")));

        var monitor = Substitute.For<IMessageMonitor>();
        var serializationFactory = Substitute.For<IMessageBodySerializationFactory>();

        using var bus = new JustSayingBus(config, serializationFactory,
            NullLoggerFactory.Instance, monitor);
        bus.AddMessagePublisher<SimpleMessage>(publisher);

        var messages = new List<Message>
        {
            new SimpleMessage { Id = Guid.NewGuid() },
            new SimpleMessage { Id = Guid.NewGuid() }
        };

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            () => bus.PublishAsync(messages, null, CancellationToken.None));
        tracerProvider.ForceFlush();

        // Assert
        var activity = exportedActivities.FirstOrDefault(a => a.OperationName.Contains("publish"));
        activity.ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.Events.ShouldContain(e => e.Name == "exception");
    }

    private class SimpleMessage : Message { }
}
