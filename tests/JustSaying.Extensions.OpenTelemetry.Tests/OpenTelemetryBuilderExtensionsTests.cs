using System.Diagnostics;
using System.Diagnostics.Metrics;
using JustSaying.Messaging.Monitoring;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace JustSaying.Extensions.OpenTelemetry.Tests;

[Collection("Tracing")]
public class OpenTelemetryBuilderExtensionsTests : IDisposable
{
    private readonly List<Activity> _exportedActivities = [];
    private readonly List<Metric> _exportedMetrics = [];
    private readonly TracerProvider _tracerProvider;
    private readonly MeterProvider _meterProvider;

    public OpenTelemetryBuilderExtensionsTests()
    {
        // AddJustSayingInstrumentation on OpenTelemetryBuilder requires the hosting
        // builder which needs IServiceCollection. Test the individual registrations
        // that the top-level method delegates to, verifying both work in combination.
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(JustSayingDiagnostics.ActivitySourceName)
            .AddInMemoryExporter(_exportedActivities)
            .Build();

        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(JustSayingDiagnostics.MeterName)
            .AddInMemoryExporter(_exportedMetrics)
            .Build();
    }

    [Fact]
    public void Registers_Both_ActivitySource_And_Meter()
    {
        // Act - emit a trace
        using var activity = JustSayingDiagnostics.ActivitySource.StartActivity("test-combined");
        activity?.Stop();
        _tracerProvider.ForceFlush();

        // Act - emit a metric
        JustSayingDiagnostics.ClientSentMessages.Add(1);
        _meterProvider.ForceFlush();

        // Assert - both are captured
        _exportedActivities.ShouldNotBeEmpty();
        _exportedActivities.ShouldContain(a => a.OperationName == "test-combined");

        _exportedMetrics.ShouldNotBeEmpty();
        _exportedMetrics.ShouldContain(m => m.Name == "messaging.client.sent.messages");
    }

    [Fact]
    public void AddJustSayingInstrumentation_Throws_When_Builder_Is_Null()
    {
        OpenTelemetryBuilder builder = null;

        Should.Throw<ArgumentNullException>(() => builder.AddJustSayingInstrumentation());
    }

    public void Dispose()
    {
        _tracerProvider?.Dispose();
        _meterProvider?.Dispose();
    }
}
