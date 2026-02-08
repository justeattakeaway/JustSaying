using System.Diagnostics;
using JustSaying.Messaging;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Tracing;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Middleware.Tracing;

public class TracingPublishMiddlewareTests : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = new();

    public TracingPublishMiddlewareTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "JustSaying.MessagePublisher",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public async Task SingleMessage_CreatesProducerActivity()
    {
        var middleware = new TracingPublishMiddleware();
        var context = new PublishContext(new OrderAccepted(), new PublishMetadata());

        var result = await middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None);

        result.ShouldBeTrue();

        var activity = _activities.ShouldHaveSingleItem();
        activity.OperationName.ShouldBe("publish OrderAccepted");
        activity.Kind.ShouldBe(ActivityKind.Producer);
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public async Task SingleMessage_SetsMessagingTags()
    {
        var middleware = new TracingPublishMiddleware();
        var context = new PublishContext(new OrderAccepted(), new PublishMetadata());

        await middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None);

        var activity = _activities.ShouldHaveSingleItem();
        activity.GetTagItem("messaging.system").ShouldBe("aws_sns");
        activity.GetTagItem("messaging.operation").ShouldBe("publish");
        activity.GetTagItem("messaging.message.type").ShouldBe("OrderAccepted");
    }

    [Fact]
    public async Task SingleMessage_InjectsTraceContextIntoMetadata()
    {
        var middleware = new TracingPublishMiddleware();
        var metadata = new PublishMetadata();
        var context = new PublishContext(new OrderAccepted(), metadata);

        await middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None);

        var traceparent = metadata.MessageAttributes?["traceparent"];
        traceparent.ShouldNotBeNull();
        traceparent.StringValue.ShouldNotBeNullOrEmpty();
        traceparent.StringValue.ShouldStartWith("00-");
    }

    [Fact]
    public async Task SingleMessage_WithMessageId_SetsMessageIdTagAndAttribute()
    {
        var middleware = new TracingPublishMiddleware();
        var message = new OrderAccepted { Id = Guid.NewGuid() };
        var metadata = new PublishMetadata();
        var context = new PublishContext(message, metadata);

        await middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None);

        var activity = _activities.ShouldHaveSingleItem();
        activity.GetTagItem("messaging.message.id").ShouldBe(message.Id.ToString());

        metadata.MessageAttributes["message_id"].StringValue.ShouldBe(message.Id.ToString());
    }

    [Fact]
    public async Task BatchMessages_CreatesBatchActivity()
    {
        var middleware = new TracingPublishMiddleware();
        var messages = new List<JustSaying.Models.Message> { new OrderAccepted(), new OrderAccepted() };
        var context = new PublishContext(messages.AsReadOnly(), new PublishMetadata());

        await middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None);

        var activity = _activities.ShouldHaveSingleItem();
        activity.OperationName.ShouldBe("publish batch OrderAccepted");
        activity.GetTagItem("messaging.batch.message_count").ShouldBe(2);
    }

    [Fact]
    public async Task OnException_SetsErrorStatusAndRecordsEvent()
    {
        var middleware = new TracingPublishMiddleware();
        var context = new PublishContext(new OrderAccepted(), new PublishMetadata());

        var exception = new InvalidOperationException("test failure");
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => middleware.RunAsync(context, _ => throw exception, CancellationToken.None));

        thrown.ShouldBe(exception);

        var activity = _activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("test failure");

        var exceptionEvent = activity.Events.ShouldHaveSingleItem();
        exceptionEvent.Name.ShouldBe("exception");
        exceptionEvent.Tags.First(t => t.Key == "exception.type").Value.ShouldBe("System.InvalidOperationException");
    }

    [Fact]
    public async Task WhenNoListenerAttached_StillCallsInnerFunc()
    {
        _listener.Dispose();
        _activities.Clear();

        var middleware = new TracingPublishMiddleware();
        var context = new PublishContext(new OrderAccepted(), new PublishMetadata());

        var result = await middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None);

        result.ShouldBeTrue();
    }
}
