using System.Diagnostics;
using JustSaying.Extensions.OpenTelemetry;
using JustSaying.Messaging.Monitoring;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace JustSaying.Extensions.OpenTelemetry.Tests;

[NotInParallel("Tracing")]
public class TracerProviderBuilderExtensionsTests
{
    [Test]
    public void AddJustSayingInstrumentation_Registers_ActivitySource()
    {
        // Arrange
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedActivities)
            .Build();

        // Act
        using var activity = JustSayingDiagnostics.ActivitySource.StartActivity("test-operation");
        activity?.Stop();
        tracerProvider.ForceFlush();

        // Assert
        exportedActivities.ShouldNotBeEmpty();
        exportedActivities.ShouldContain(a => a.OperationName == "test-operation");
    }

    [Test]
    public void AddJustSayingInstrumentation_Throws_When_Builder_Is_Null()
    {
        TracerProviderBuilder builder = null;

        Should.Throw<ArgumentNullException>(() => builder.AddJustSayingInstrumentation());
    }
}
