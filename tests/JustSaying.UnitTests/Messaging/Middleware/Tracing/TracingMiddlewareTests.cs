using System.Collections.Concurrent;
using System.Diagnostics;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Tracing;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using Microsoft.Extensions.DependencyInjection;
using MessageAttributeValue = JustSaying.Messaging.MessageAttributeValue;
using SqsMessage = Amazon.SQS.Model.Message;

namespace JustSaying.UnitTests.Messaging.Middleware.Tracing;

public class TracingMiddlewareTests : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly ConcurrentBag<Activity> _activities = new();

    public TracingMiddlewareTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "JustSaying.MessageHandler",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    /// <summary>
    /// Finds the single activity created during a test action. Because ActivityListener is
    /// process-global, parallel test classes may add activities to our bag. We snapshot before
    /// the action and filter by operation name afterwards to isolate our test's activity.
    /// </summary>
    private async Task<Activity> CaptureActivityAsync(Func<Task> action)
    {
        var before = _activities.ToHashSet();
        await action();
        var newActivities = _activities
            .Where(a => !before.Contains(a) && a.OperationName == "process OrderAccepted")
            .ToList();
        return newActivities.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CreatesConsumerActivity()
    {
        var middleware = new TracingMiddleware(new TracingOptions());
        var context = ContextWithAttributes();
        bool result = false;

        var activity = await CaptureActivityAsync(async () =>
            result = await middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None));

        result.ShouldBeTrue();
        activity.OperationName.ShouldBe("process OrderAccepted");
        activity.Kind.ShouldBe(ActivityKind.Consumer);
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public async Task SetsMessagingTags()
    {
        var middleware = new TracingMiddleware(new TracingOptions());
        var context = ContextWithAttributes();

        var activity = await CaptureActivityAsync(() =>
            middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None));

        activity.GetTagItem("messaging.system").ShouldBe("aws_sqs");
        activity.GetTagItem("messaging.operation").ShouldBe("process");
        activity.GetTagItem("messaging.destination.name").ShouldBe("test-queue");
        activity.GetTagItem("messaging.message.type").ShouldBe("OrderAccepted");
    }

    [Fact]
    public async Task LinkMode_WithTraceContext_CreatesLinkedActivity()
    {
        var middleware = new TracingMiddleware(new TracingOptions { UseParentSpan = false });
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var context = ContextWithTraceParent(traceId, spanId);

        var activity = await CaptureActivityAsync(() =>
            middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None));

        // In link mode, the activity should NOT be a child of the producer
        activity.ParentSpanId.ShouldBe(default(ActivitySpanId));

        // But should have a link to the producer span
        var link = activity.Links.ShouldHaveSingleItem();
        link.Context.TraceId.ShouldBe(traceId);
        link.Context.SpanId.ShouldBe(spanId);
    }

    [Fact]
    public async Task ParentMode_WithTraceContext_CreatesChildActivity()
    {
        var middleware = new TracingMiddleware(new TracingOptions { UseParentSpan = true });
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var context = ContextWithTraceParent(traceId, spanId);

        var activity = await CaptureActivityAsync(() =>
            middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None));

        // In parent mode, the activity should be a child of the producer
        activity.TraceId.ShouldBe(traceId);
        activity.ParentSpanId.ShouldBe(spanId);
        activity.Links.ShouldBeEmpty();
    }

    [Fact]
    public async Task WithNoTraceContext_CreatesActivityWithoutLinkOrParent()
    {
        var middleware = new TracingMiddleware(new TracingOptions());
        var context = ContextWithAttributes();

        var activity = await CaptureActivityAsync(() =>
            middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None));

        activity.ParentSpanId.ShouldBe(default(ActivitySpanId));
        activity.Links.ShouldBeEmpty();
    }

    [Fact]
    public async Task WithMessageIdAttribute_SetsMessageIdTag()
    {
        var messageId = Guid.NewGuid().ToString();
        var attributes = new Dictionary<string, MessageAttributeValue>
        {
            ["message_id"] = new() { StringValue = messageId, DataType = "String" }
        };
        var context = ContextWithAttributes(attributes);
        var middleware = new TracingMiddleware(new TracingOptions());

        var activity = await CaptureActivityAsync(() =>
            middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None));

        activity.GetTagItem("messaging.message.id").ShouldBe(messageId);
    }

    [Fact]
    public async Task WhenHandlerReturnsFalse_SetsErrorStatus()
    {
        var middleware = new TracingMiddleware(new TracingOptions());
        var context = ContextWithAttributes();
        bool result = true;

        var activity = await CaptureActivityAsync(async () =>
            result = await middleware.RunAsync(context, _ => Task.FromResult(false), CancellationToken.None));

        result.ShouldBeFalse();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public async Task OnException_SetsErrorStatusAndRecordsEvent()
    {
        var middleware = new TracingMiddleware(new TracingOptions());
        var context = ContextWithAttributes();

        var exception = new InvalidOperationException("handler failed");
        var before = _activities.ToHashSet();

        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => middleware.RunAsync(context, _ => throw exception, CancellationToken.None));

        thrown.ShouldBe(exception);

        var activity = _activities
            .Where(a => !before.Contains(a) && a.OperationName == "process OrderAccepted")
            .ToList().ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);

        var exceptionEvent = activity.Events.ShouldHaveSingleItem();
        exceptionEvent.Name.ShouldBe("exception");
        exceptionEvent.Tags.First(t => t.Key == "exception.type").Value.ShouldBe("System.InvalidOperationException");
    }

    [Fact]
    public async Task WhenNoListenerAttached_StillCallsInnerFunc()
    {
        _listener.Dispose();

        var middleware = new TracingMiddleware(new TracingOptions());
        var context = ContextWithAttributes();

        var result = await middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ParentMode_WithNoTraceContext_CreatesActivityWithoutParent()
    {
        var middleware = new TracingMiddleware(new TracingOptions { UseParentSpan = true });
        var context = ContextWithAttributes();

        var activity = await CaptureActivityAsync(() =>
            middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None));

        activity.ParentSpanId.ShouldBe(default(ActivitySpanId));
        activity.Links.ShouldBeEmpty();
    }

    [Fact]
    public async Task UseTracingMiddleware_ExtensionRegistersMiddleware()
    {
        var resolver = new InMemoryServiceResolver(c =>
            c.AddSingleton(new TracingOptions())
                .AddTransient<TracingMiddleware>());

        var builder = new HandlerMiddlewareBuilder(resolver, resolver)
            .UseTracingMiddleware()
            .UseHandler(_ => new InspectableHandler<OrderAccepted>());

        var middleware = builder.Build();
        var context = ContextWithAttributes();

        var activity = await CaptureActivityAsync(() =>
            middleware.RunAsync(context, null, CancellationToken.None));

        activity.ShouldNotBeNull();
    }

    private static HandleMessageContext ContextWithAttributes(
        Dictionary<string, MessageAttributeValue> attributes = null)
    {
        return new HandleMessageContext(
            "test-queue",
            new SqsMessage(),
            new OrderAccepted(),
            typeof(OrderAccepted),
            new FakeVisibilityUpdater(),
            new FakeMessageDeleter(),
            new Uri("http://test-queue"),
            new MessageAttributes(attributes ?? new Dictionary<string, MessageAttributeValue>()));
    }

    private static HandleMessageContext ContextWithTraceParent(
        ActivityTraceId traceId, ActivitySpanId spanId)
    {
        var traceparent = $"00-{traceId}-{spanId}-01";
        var attributes = new Dictionary<string, MessageAttributeValue>
        {
            ["traceparent"] = new() { StringValue = traceparent, DataType = "String" }
        };
        return ContextWithAttributes(attributes);
    }
}
