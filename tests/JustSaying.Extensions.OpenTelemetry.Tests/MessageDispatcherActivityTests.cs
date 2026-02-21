using System.Diagnostics;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Extensions.OpenTelemetry;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OpenTelemetry;
using OpenTelemetry.Trace;
#pragma warning disable CS0618
using JustSaying.Messaging.Middleware.Tracing;
#pragma warning restore CS0618
using SqsMessage = Amazon.SQS.Model.Message;

namespace JustSaying.Extensions.OpenTelemetry.Tests;

[Collection("Tracing")]
public class MessageDispatcherActivityTests
{
    [Fact]
    public async Task DispatchMessage_Creates_Consumer_Activity_With_Parent_From_TraceParent()
    {
        // Arrange
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedActivities)
            .Build();

        // Construct a valid W3C traceparent with the Recorded flag set
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceParent = $"00-{traceId}-{spanId}-01";

        var middlewareMap = new MiddlewareMap();
        var fakeMiddleware = new FakeMiddleware();
        middlewareMap.Add<SimpleMessage>("test-queue", fakeMiddleware);

        var monitor = Substitute.For<IMessageMonitor>();

        var dispatcher = new MessageDispatcher(
            monitor,
            middlewareMap,
            NullLoggerFactory.Instance);

        var sqsMessage = new SqsMessage
        {
            MessageId = "msg-123",
            Body = "{}"
        };

        var messageAttributes = new MessageAttributes(new Dictionary<string, MessageAttributeValue>
        {
            [MessageAttributeKeys.TraceParent] = new() { DataType = "String", StringValue = traceParent }
        });

        var inboundMessage = new InboundMessage(new SimpleMessage { Id = Guid.NewGuid() }, messageAttributes);

        var messageConverter = Substitute.For<IInboundMessageConverter>();
        messageConverter.ConvertToInboundMessageAsync(sqsMessage, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<InboundMessage>(inboundMessage));

        var messageContext = Substitute.For<IQueueMessageContext>();
        messageContext.Message.Returns(sqsMessage);
        messageContext.QueueName.Returns("test-queue");
        messageContext.QueueUri.Returns(new Uri("https://sqs.eu-west-1.amazonaws.com/123456789/test-queue"));
        messageContext.MessageConverter.Returns(messageConverter);

        // Act
        await dispatcher.DispatchMessageAsync(messageContext, CancellationToken.None);
        tracerProvider.ForceFlush();

        // Assert
        var consumerActivity = exportedActivities.FirstOrDefault(a => a.OperationName == "test-queue process");
        consumerActivity.ShouldNotBeNull();
        consumerActivity.Kind.ShouldBe(ActivityKind.Consumer);
        consumerActivity.ParentId.ShouldBeNull(); // Uses links, not parent-child
        var links = consumerActivity.Links.ToList();
        links.Count.ShouldBe(1);
        links[0].Context.TraceId.ShouldBe(traceId);
        links[0].Context.SpanId.ShouldBe(spanId);
        consumerActivity.GetTagItem("messaging.system").ShouldBe("aws_sqs");
        consumerActivity.GetTagItem("messaging.destination.name").ShouldBe("test-queue");
        consumerActivity.GetTagItem("messaging.operation.name").ShouldBe("process");
        consumerActivity.GetTagItem("messaging.operation.type").ShouldBe("process");
        consumerActivity.GetTagItem("messaging.message.id").ShouldBe("msg-123");
        consumerActivity.GetTagItem("messaging.message.type").ShouldBe(typeof(SimpleMessage).FullName);
    }

    [Fact]
    public async Task DispatchMessage_Creates_Activity_Without_Parent_When_No_TraceParent()
    {
        // Arrange
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedActivities)
            .Build();

        var middlewareMap = new MiddlewareMap();
        var fakeMiddleware = new FakeMiddleware();
        middlewareMap.Add<SimpleMessage>("test-queue", fakeMiddleware);

        var monitor = Substitute.For<IMessageMonitor>();

        var dispatcher = new MessageDispatcher(
            monitor,
            middlewareMap,
            NullLoggerFactory.Instance);

        var sqsMessage = new SqsMessage
        {
            MessageId = "msg-456",
            Body = "{}"
        };

        var messageAttributes = new MessageAttributes();

        var inboundMessage = new InboundMessage(new SimpleMessage { Id = Guid.NewGuid() }, messageAttributes);

        var messageConverter = Substitute.For<IInboundMessageConverter>();
        messageConverter.ConvertToInboundMessageAsync(sqsMessage, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<InboundMessage>(inboundMessage));

        var messageContext = Substitute.For<IQueueMessageContext>();
        messageContext.Message.Returns(sqsMessage);
        messageContext.QueueName.Returns("test-queue");
        messageContext.QueueUri.Returns(new Uri("https://sqs.eu-west-1.amazonaws.com/123456789/test-queue"));
        messageContext.MessageConverter.Returns(messageConverter);

        // Act
        Activity.Current = null;
        await dispatcher.DispatchMessageAsync(messageContext, CancellationToken.None);
        tracerProvider.ForceFlush();

        // Assert
        var consumerActivity = exportedActivities.FirstOrDefault(a => a.OperationName == "test-queue process");
        consumerActivity.ShouldNotBeNull();
        consumerActivity.ParentId.ShouldBeNull();
    }

    [Fact]
    public async Task DispatchMessage_Records_Error_Status_On_Middleware_Failure()
    {
        // Arrange
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedActivities)
            .Build();

        var middlewareMap = new MiddlewareMap();
        var throwingMiddleware = new ThrowingMiddleware();
        middlewareMap.Add<SimpleMessage>("test-queue", throwingMiddleware);

        var monitor = Substitute.For<IMessageMonitor>();

        var dispatcher = new MessageDispatcher(
            monitor,
            middlewareMap,
            NullLoggerFactory.Instance);

        var sqsMessage = new SqsMessage
        {
            MessageId = "msg-error",
            Body = "{}"
        };

        var messageAttributes = new MessageAttributes();
        var inboundMessage = new InboundMessage(new SimpleMessage { Id = Guid.NewGuid() }, messageAttributes);

        var messageConverter = Substitute.For<IInboundMessageConverter>();
        messageConverter.ConvertToInboundMessageAsync(sqsMessage, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<InboundMessage>(inboundMessage));

        var messageContext = Substitute.For<IQueueMessageContext>();
        messageContext.Message.Returns(sqsMessage);
        messageContext.QueueName.Returns("test-queue");
        messageContext.QueueUri.Returns(new Uri("https://sqs.eu-west-1.amazonaws.com/123456789/test-queue"));
        messageContext.MessageConverter.Returns(messageConverter);

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            () => dispatcher.DispatchMessageAsync(messageContext, CancellationToken.None));
        tracerProvider.ForceFlush();

        // Assert
        var consumerActivity = exportedActivities.FirstOrDefault(a => a.OperationName == "test-queue process");
        consumerActivity.ShouldNotBeNull();
        consumerActivity.Status.ShouldBe(ActivityStatusCode.Error);
        consumerActivity.Events.ShouldContain(e => e.Name == "exception");
    }

    [Fact]
    public async Task DispatchMessage_UseParentSpan_Creates_Child_Span_With_Same_TraceId()
    {
        // Arrange
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedActivities)
            .Build();

        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceParent = $"00-{traceId}-{spanId}-01";

        var middlewareMap = new MiddlewareMap();
        middlewareMap.Add<SimpleMessage>("test-queue", new FakeMiddleware());

        var monitor = Substitute.For<IMessageMonitor>();

#pragma warning disable CS0618
        var tracingOptions = new TracingOptions { UseParentSpan = true };
#pragma warning restore CS0618

        var dispatcher = new MessageDispatcher(
            monitor,
            middlewareMap,
            NullLoggerFactory.Instance,
            tracingOptions);

        var sqsMessage = new SqsMessage { MessageId = "msg-parent", Body = "{}" };

        var messageAttributes = new MessageAttributes(new Dictionary<string, MessageAttributeValue>
        {
            [MessageAttributeKeys.TraceParent] = new() { DataType = "String", StringValue = traceParent }
        });

        var inboundMessage = new InboundMessage(new SimpleMessage { Id = Guid.NewGuid() }, messageAttributes);

        var messageConverter = Substitute.For<IInboundMessageConverter>();
        messageConverter.ConvertToInboundMessageAsync(sqsMessage, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<InboundMessage>(inboundMessage));

        var messageContext = Substitute.For<IQueueMessageContext>();
        messageContext.Message.Returns(sqsMessage);
        messageContext.QueueName.Returns("test-queue");
        messageContext.QueueUri.Returns(new Uri("https://sqs.eu-west-1.amazonaws.com/123456789/test-queue"));
        messageContext.MessageConverter.Returns(messageConverter);

        // Act
        await dispatcher.DispatchMessageAsync(messageContext, CancellationToken.None);
        tracerProvider.ForceFlush();

        // Assert — consumer span is a child of the producer span (same traceId, parentSpanId set)
        var consumerActivity = exportedActivities.FirstOrDefault(a => a.OperationName == "test-queue process");
        consumerActivity.ShouldNotBeNull();
        consumerActivity.Kind.ShouldBe(ActivityKind.Consumer);
        consumerActivity.TraceId.ShouldBe(traceId);
        consumerActivity.ParentSpanId.ShouldBe(spanId);
        consumerActivity.Links.ShouldBeEmpty(); // parent mode uses no links
    }

    private class SimpleMessage : JustSaying.Models.Message { }

    private class FakeMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        protected override Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            return Task.FromResult(true);
        }
    }

    private class ThrowingMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        protected override Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            throw new InvalidOperationException("Handler failed");
        }
    }
}
