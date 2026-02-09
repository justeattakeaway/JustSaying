using System.Collections.Concurrent;
using System.Diagnostics;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Tracing;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenPublishingWithTracePropagation : IntegrationTestBase, IDisposable
{
    private readonly ConcurrentBag<Activity> _activities = [];
    private readonly ActivityListener _listener;

    public WhenPublishingWithTracePropagation(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "JustSaying.MessagePublisher" or "JustSaying.MessageHandler",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity),
            ActivityStopped = _ => { }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    [AwsFact]
    public async Task Then_Trace_Context_Is_Propagated_With_Link()
    {
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .AddTransient<TracingPublishMiddleware>()
            .AddSingleton(new TracingOptions { UseParentSpan = false })
            .AddTransient<TracingMiddleware>()
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler)
            .ConfigureJustSaying(builder =>
            {
                builder.Publications(pub => pub
                    .WithTopic<SimpleMessage>(cfg => cfg
                        .WithMiddlewareConfiguration(pipe => pipe.Use<TracingPublishMiddleware>())));
                builder.Subscriptions(sub => sub
                    .ForTopic<SimpleMessage>(cfg =>
                    {
                        cfg.WithQueueName(UniqueName);
                        cfg.WithMiddlewareConfiguration(pipe =>
                        {
                            pipe.Use<TracingMiddleware>();
                            pipe.UseHandler<SimpleMessage>();
                        });
                    }));
            });

        await WhenAsync(services, async (publisher, listener, cancellationToken) =>
        {
            await listener.StartAsync(cancellationToken);
            await publisher.StartAsync(cancellationToken);

            await publisher.PublishAsync(new SimpleMessage { Content = "trace-test" }, cancellationToken);

            // Wait for handler to process
            await Patiently.AssertThatAsync(OutputHelper,
                () => handler.ReceivedMessages.Count.ShouldBe(1));
        });

        // Verify both producer and consumer activities were created
        var producerActivity = _activities.SingleOrDefault(a => a.Kind == ActivityKind.Producer);
        var consumerActivity = _activities.SingleOrDefault(a => a.Kind == ActivityKind.Consumer);

        producerActivity.ShouldNotBeNull("Expected a Producer activity from TracingPublishMiddleware");
        consumerActivity.ShouldNotBeNull("Expected a Consumer activity from TracingMiddleware");

        producerActivity.OperationName.ShouldBe("publish SimpleMessage");
        consumerActivity.OperationName.ShouldBe("process SimpleMessage");

        // In link mode, the consumer should have a different trace but link back to the producer
        consumerActivity.TraceId.ShouldNotBe(producerActivity.TraceId);
        var link = consumerActivity.Links.ShouldHaveSingleItem();
        link.Context.TraceId.ShouldBe(producerActivity.TraceId);
        link.Context.SpanId.ShouldBe(producerActivity.SpanId);
    }

    [AwsFact]
    public async Task Then_Trace_Context_Is_Propagated_With_Parent_Span()
    {
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .AddTransient<TracingPublishMiddleware>()
            .AddSingleton(new TracingOptions { UseParentSpan = true })
            .AddTransient<TracingMiddleware>()
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler)
            .ConfigureJustSaying(builder =>
            {
                builder.Publications(pub => pub
                    .WithTopic<SimpleMessage>(cfg => cfg
                        .WithMiddlewareConfiguration(pipe => pipe.Use<TracingPublishMiddleware>())));
                builder.Subscriptions(sub => sub
                    .ForTopic<SimpleMessage>(cfg =>
                    {
                        cfg.WithQueueName(UniqueName);
                        cfg.WithMiddlewareConfiguration(pipe =>
                        {
                            pipe.Use<TracingMiddleware>();
                            pipe.UseHandler<SimpleMessage>();
                        });
                    }));
            });

        await WhenAsync(services, async (publisher, listener, cancellationToken) =>
        {
            await listener.StartAsync(cancellationToken);
            await publisher.StartAsync(cancellationToken);

            await publisher.PublishAsync(new SimpleMessage { Content = "parent-trace-test" }, cancellationToken);

            // Wait for handler to process
            await Patiently.AssertThatAsync(OutputHelper,
                () => handler.ReceivedMessages.Count.ShouldBe(1));
        });

        // Verify both producer and consumer activities were created
        var producerActivity = _activities.SingleOrDefault(a => a.Kind == ActivityKind.Producer);
        var consumerActivity = _activities.SingleOrDefault(a => a.Kind == ActivityKind.Consumer);

        producerActivity.ShouldNotBeNull("Expected a Producer activity from TracingPublishMiddleware");
        consumerActivity.ShouldNotBeNull("Expected a Consumer activity from TracingMiddleware");

        // In parent mode, the consumer should share the same trace and be a child of the producer
        consumerActivity.TraceId.ShouldBe(producerActivity.TraceId);
        consumerActivity.ParentSpanId.ShouldBe(producerActivity.SpanId);
        consumerActivity.Links.ShouldBeEmpty();
    }
}
