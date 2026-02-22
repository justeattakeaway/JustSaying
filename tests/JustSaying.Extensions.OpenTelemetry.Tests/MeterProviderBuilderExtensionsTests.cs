using System.Diagnostics.Metrics;
using JustSaying.Extensions.OpenTelemetry;
using JustSaying.Messaging.Monitoring;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace JustSaying.Extensions.OpenTelemetry.Tests;

[Collection("Metrics")]
public class MeterProviderBuilderExtensionsTests
{
    [Fact]
    public void AddJustSayingInstrumentation_Registers_Meter()
    {
        // Arrange
        var exportedMetrics = new List<Metric>();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedMetrics)
            .Build();

        // Act
        JustSayingDiagnostics.ClientSentMessages.Add(1);
        meterProvider.ForceFlush();

        // Assert
        exportedMetrics.ShouldNotBeEmpty();
        exportedMetrics.ShouldContain(m => m.Name == "messaging.client.sent.messages");
    }

    [Fact]
    public void AddJustSayingInstrumentation_Throws_When_Builder_Is_Null()
    {
        MeterProviderBuilder builder = null;

        Should.Throw<ArgumentNullException>(() => builder.AddJustSayingInstrumentation());
    }
}
