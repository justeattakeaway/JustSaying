using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Tracing;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Tracing;

public class TraceContextPropagatorTests
{
    [Fact]
    public void InjectTraceContext_AddsTraceParentHeader()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = KafkaActivitySource.Source.StartActivity("test");
        var headers = new Headers();

        // Act
        TraceContextPropagator.InjectTraceContext(headers, activity);

        // Assert
        var traceParentHeader = headers.FirstOrDefault(h => h.Key == TraceContextPropagator.TraceParentHeader);
        traceParentHeader.ShouldNotBeNull();

        var traceParent = Encoding.UTF8.GetString(traceParentHeader.GetValueBytes());
        traceParent.ShouldStartWith("00-"); // Version
        traceParent.Split('-').Length.ShouldBe(4); // version-traceid-spanid-flags
    }

    [Fact]
    public void InjectTraceContext_UsesCurrentActivityWhenNotProvided()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = KafkaActivitySource.Source.StartActivity("test");
        Activity.Current = activity;
        
        var headers = new Headers();

        // Act
        TraceContextPropagator.InjectTraceContext(headers);

        // Assert
        var traceParentHeader = headers.FirstOrDefault(h => h.Key == TraceContextPropagator.TraceParentHeader);
        traceParentHeader.ShouldNotBeNull();
    }

    [Fact]
    public void InjectTraceContext_HandlesNullHeaders()
    {
        // Act & Assert - should not throw
        Should.NotThrow(() => TraceContextPropagator.InjectTraceContext(null));
    }

    [Fact]
    public void InjectTraceContext_HandlesNullActivity()
    {
        // Arrange
        var headers = new Headers();
        Activity.Current = null;

        // Act
        TraceContextPropagator.InjectTraceContext(headers, null);

        // Assert - no headers should be added
        headers.Count.ShouldBe(0);
    }

    [Fact]
    public void ExtractTraceContext_ReturnsContextFromHeaders()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceParent = $"00-{traceId}-{spanId}-01";

        var headers = new Headers();
        headers.Add(TraceContextPropagator.TraceParentHeader, Encoding.UTF8.GetBytes(traceParent));

        // Act
        var context = TraceContextPropagator.ExtractTraceContext(headers);

        // Assert
        context.ShouldNotBeNull();
        context.Value.TraceId.ShouldBe(traceId);
        context.Value.SpanId.ShouldBe(spanId);
        context.Value.TraceFlags.ShouldBe(ActivityTraceFlags.Recorded);
    }

    [Fact]
    public void ExtractTraceContext_ReturnsNullForMissingHeaders()
    {
        // Arrange
        var headers = new Headers();

        // Act
        var context = TraceContextPropagator.ExtractTraceContext(headers);

        // Assert
        context.ShouldBeNull();
    }

    [Fact]
    public void ExtractTraceContext_ReturnsNullForInvalidTraceParent()
    {
        // Arrange
        var headers = new Headers();
        headers.Add(TraceContextPropagator.TraceParentHeader, Encoding.UTF8.GetBytes("invalid"));

        // Act
        var context = TraceContextPropagator.ExtractTraceContext(headers);

        // Assert
        context.ShouldBeNull();
    }

    [Fact]
    public void ExtractTraceContext_HandlesNullHeaders()
    {
        // Act
        var context = TraceContextPropagator.ExtractTraceContext((Headers)null);

        // Assert
        context.ShouldBeNull();
    }

    [Fact]
    public void ExtractTraceContext_IncludesTraceState()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceParent = $"00-{traceId}-{spanId}-01";
        var traceState = "vendor1=value1,vendor2=value2";

        var headers = new Headers();
        headers.Add(TraceContextPropagator.TraceParentHeader, Encoding.UTF8.GetBytes(traceParent));
        headers.Add(TraceContextPropagator.TraceStateHeader, Encoding.UTF8.GetBytes(traceState));

        // Act
        var context = TraceContextPropagator.ExtractTraceContext(headers);

        // Assert
        context.ShouldNotBeNull();
        context.Value.TraceState.ShouldBe(traceState);
    }

    [Fact]
    public void ExtractTraceContext_FromDictionary_Works()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceParent = $"00-{traceId}-{spanId}-00";

        var headers = new Dictionary<string, string>
        {
            [TraceContextPropagator.TraceParentHeader] = traceParent
        };

        // Act
        var context = TraceContextPropagator.ExtractTraceContext(headers);

        // Assert
        context.ShouldNotBeNull();
        context.Value.TraceId.ShouldBe(traceId);
        context.Value.TraceFlags.ShouldBe(ActivityTraceFlags.None);
    }

    [Fact]
    public void RoundTrip_InjectAndExtract_PreservesContext()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var originalActivity = KafkaActivitySource.Source.StartActivity("test");
        var headers = new Headers();

        // Act - inject
        TraceContextPropagator.InjectTraceContext(headers, originalActivity);

        // Act - extract
        var extractedContext = TraceContextPropagator.ExtractTraceContext(headers);

        // Assert
        extractedContext.ShouldNotBeNull();
        extractedContext.Value.TraceId.ShouldBe(originalActivity.TraceId);
        extractedContext.Value.SpanId.ShouldBe(originalActivity.SpanId);
    }
}
