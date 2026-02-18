using System.Diagnostics;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using NSubstitute;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace JustSaying.Extensions.OpenTelemetry.Tests;

[Collection("Tracing")]
public class TraceContextPropagationTests
{
    [Fact]
    public async Task Publish_Injects_TraceParent_Into_Message_Attributes()
    {
        // Arrange
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedActivities)
            .Build();

        var serializer = Substitute.For<IMessageBodySerializer>();
        serializer.Serialize(Arg.Any<Message>()).Returns("{}");

        var converter = new OutboundMessageConverter(
            PublishDestinationType.Topic,
            serializer,
            new MessageCompressionRegistry([]),
            null,
            "TestSubject",
            false);

        // Act - start an activity to create trace context
        using var parentActivity = JustSayingDiagnostics.ActivitySource.StartActivity("test-publish", ActivityKind.Producer);
        parentActivity.ShouldNotBeNull();

        var message = new SimpleMessage { Id = Guid.NewGuid() };
        var result = await converter.ConvertToOutboundMessageAsync(message, null);

        // Assert
        result.MessageAttributes.ShouldContainKey(MessageAttributeKeys.TraceParent);
        result.MessageAttributes[MessageAttributeKeys.TraceParent].StringValue.ShouldNotBeNullOrEmpty();
        result.MessageAttributes[MessageAttributeKeys.TraceParent].DataType.ShouldBe("String");
    }

    [Fact]
    public async Task Publish_Does_Not_Inject_TraceParent_When_No_Activity()
    {
        // Arrange - ensure no active activity
        Activity.Current = null;

        var serializer = Substitute.For<IMessageBodySerializer>();
        serializer.Serialize(Arg.Any<Message>()).Returns("{}");

        var converter = new OutboundMessageConverter(
            PublishDestinationType.Topic,
            serializer,
            new MessageCompressionRegistry([]),
            null,
            "TestSubject",
            false);

        // Act
        var message = new SimpleMessage { Id = Guid.NewGuid() };
        var result = await converter.ConvertToOutboundMessageAsync(message, null);

        // Assert
        result.MessageAttributes.ShouldNotContainKey(MessageAttributeKeys.TraceParent);
    }

    private class SimpleMessage : JustSaying.Models.Message { }
}
