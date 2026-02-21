using System.Diagnostics;
using System.Diagnostics.Metrics;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public void AddJustSayingInstrumentation_Registers_Via_OpenTelemetryBuilder()
    {
        // Arrange - use IServiceCollection to get a real OpenTelemetryBuilder
        var services = new ServiceCollection();
        services.AddOpenTelemetry().AddJustSayingInstrumentation();

        using var serviceProvider = services.BuildServiceProvider();

        // Force the TracerProvider and MeterProvider to be resolved
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        var meterProvider = serviceProvider.GetService<MeterProvider>();

        // Assert - providers were created (the extension registered sources/meters)
        tracerProvider.ShouldNotBeNull();
        meterProvider.ShouldNotBeNull();
    }

    [Fact]
    public void AddJustSayingInstrumentation_Captures_Activities_Via_OpenTelemetryBuilder()
    {
        // Arrange - wire up via the OpenTelemetryBuilder path (the IServiceCollection / hosting path)
        var exportedActivities = new List<Activity>();
        var exportedMetrics = new List<Metric>();

        var services = new ServiceCollection();
        services.AddOpenTelemetry()
            .AddJustSayingInstrumentation()
            .WithTracing(b => b.AddInMemoryExporter(exportedActivities))
            .WithMetrics(b => b.AddInMemoryExporter(exportedMetrics));

        using var serviceProvider = services.BuildServiceProvider();
        var tracerProvider = serviceProvider.GetRequiredService<TracerProvider>();
        var meterProvider = serviceProvider.GetRequiredService<MeterProvider>();

        // Act — emit a trace and a metric from JustSayingDiagnostics
        using (var activity = JustSayingDiagnostics.ActivitySource.StartActivity("test-builder-e2e"))
        {
            activity?.Stop();
        }

        JustSayingDiagnostics.ClientSentMessages.Add(1);

        tracerProvider.ForceFlush();
        meterProvider.ForceFlush();

        // Assert — both are captured via the OpenTelemetryBuilder path
        exportedActivities.ShouldContain(a => a.OperationName == "test-builder-e2e",
            "JustSayingDiagnostics.ActivitySource should be subscribed via AddJustSayingInstrumentation");
        exportedMetrics.ShouldContain(m => m.Name == "messaging.client.sent.messages",
            "JustSayingDiagnostics.Meter should be subscribed via AddJustSayingInstrumentation");
    }

    public void Dispose()
    {
        _tracerProvider?.Dispose();
        _meterProvider?.Dispose();
    }
}
