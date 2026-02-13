using System.Collections.Concurrent;
using System.Diagnostics;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware.Tracing;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenPublishingWithTracePropagation(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_Trace_Context_Is_Propagated_With_Link()
    {
        var handler = new InspectableHandler<SimpleMessage>();
        var activities = new ConcurrentBag<Activity>();

        using var activityListener = CreateListener(activities);

        var services = GivenJustSaying()
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler)
            .ConfigureJustSaying(builder =>
                builder.WithLoopbackTopic<SimpleMessage>(UniqueName));

        await WhenAsync(services, async (publisher, listener, cancellationToken) =>
        {
            await listener.StartAsync(cancellationToken);
            await publisher.StartAsync(cancellationToken);

            await publisher.PublishAsync(new SimpleMessage { Content = "trace-test" }, cancellationToken);

            await Patiently.AssertThatAsync(OutputHelper,
                () => handler.ReceivedMessages.Count.ShouldBe(1));

            // Wait for both activities to be captured (consumer activity may still be completing)
            await Patiently.AssertThatAsync(OutputHelper,
                () => activities.Any(a => a.OperationName == "publish SimpleMessage")
                      && activities.Any(a => a.OperationName == "process SimpleMessage"));
        });

        // Find the matched producer/consumer pair: the consumer links back to the producer.
        // We iterate all producers because ActivityListener is process-global and parallel tests
        // may contribute activities to our bag.
        var (producerActivity, consumerActivity) = FindActivityPair(activities,
            (producer, consumer) => consumer.Links.Any(l => l.Context.TraceId == producer.TraceId));

        producerActivity.Kind.ShouldBe(ActivityKind.Producer);
        consumerActivity.Kind.ShouldBe(ActivityKind.Consumer);

        // In link mode (default), the consumer should have a different trace but link back to the producer
        consumerActivity.TraceId.ShouldNotBe(producerActivity.TraceId);
        var link = consumerActivity.Links.ShouldHaveSingleItem();
        link.Context.TraceId.ShouldBe(producerActivity.TraceId);
        link.Context.SpanId.ShouldBe(producerActivity.SpanId);
    }

    [AwsFact]
    public async Task Then_Trace_Context_Is_Propagated_With_Parent_Span()
    {
        var handler = new InspectableHandler<SimpleMessage>();
        var activities = new ConcurrentBag<Activity>();

        using var activityListener = CreateListener(activities);

        var services = GivenJustSaying()
            .AddSingleton(new TracingOptions { UseParentSpan = true })
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler)
            .ConfigureJustSaying(builder =>
                builder.WithLoopbackTopic<SimpleMessage>(UniqueName));

        await WhenAsync(services, async (publisher, listener, cancellationToken) =>
        {
            await listener.StartAsync(cancellationToken);
            await publisher.StartAsync(cancellationToken);

            await publisher.PublishAsync(new SimpleMessage { Content = "parent-trace-test" }, cancellationToken);

            await Patiently.AssertThatAsync(OutputHelper,
                () => handler.ReceivedMessages.Count.ShouldBe(1));

            // Wait for both activities to be captured
            await Patiently.AssertThatAsync(OutputHelper,
                () => activities.Any(a => a.OperationName == "publish SimpleMessage")
                      && activities.Any(a => a.OperationName == "process SimpleMessage"));
        });

        // Find the matched producer/consumer pair: the consumer is a child of the producer (same trace).
        // We iterate all producers because ActivityListener is process-global and parallel tests
        // may contribute activities to our bag.
        var (producerActivity, consumerActivity) = FindActivityPair(activities,
            (producer, consumer) => consumer.TraceId == producer.TraceId);

        // In parent mode, the consumer should share the same trace and be a child of the producer
        consumerActivity.TraceId.ShouldBe(producerActivity.TraceId);
        consumerActivity.ParentSpanId.ShouldBe(producerActivity.SpanId);
        consumerActivity.Links.ShouldBeEmpty();
    }

    private static (Activity Producer, Activity Consumer) FindActivityPair(
        ConcurrentBag<Activity> activities,
        Func<Activity, Activity, bool> matchPredicate)
    {
        var producers = activities.Where(a => a.OperationName == "publish SimpleMessage");
        foreach (var producer in producers)
        {
            var consumer = activities.FirstOrDefault(a =>
                a.OperationName == "process SimpleMessage"
                && matchPredicate(producer, a));
            if (consumer != null)
            {
                return (producer, consumer);
            }
        }

        throw new InvalidOperationException(
            "No matching producer/consumer activity pair found. " +
            $"Activities captured: {string.Join(", ", activities.Select(a => $"{a.OperationName} ({a.TraceId})"))}");
    }

    private static ActivityListener CreateListener(ConcurrentBag<Activity> activities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "JustSaying.MessagePublisher" or "JustSaying.MessageHandler",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
