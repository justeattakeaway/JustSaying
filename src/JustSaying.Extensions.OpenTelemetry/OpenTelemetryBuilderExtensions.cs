using JustSaying.Extensions.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OpenTelemetry;

/// <summary>
/// Extension methods for configuring JustSaying instrumentation with OpenTelemetry.
/// </summary>
public static class OpenTelemetryBuilderJustSayingExtensions
{
    /// <summary>
    /// Adds JustSaying tracing and metrics instrumentation to the <see cref="OpenTelemetryBuilder"/>.
    /// </summary>
    /// <remarks>
    /// This is a convenience method equivalent to calling both
    /// <see cref="JustSaying.Extensions.OpenTelemetry.TracerProviderBuilderExtensions.AddJustSayingInstrumentation"/>
    /// and <see cref="JustSaying.Extensions.OpenTelemetry.MeterProviderBuilderExtensions.AddJustSayingInstrumentation"/>
    /// individually.
    /// </remarks>
    /// <param name="builder">The <see cref="OpenTelemetryBuilder"/> to configure.</param>
    /// <returns>The <see cref="OpenTelemetryBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static OpenTelemetryBuilder AddJustSayingInstrumentation(this OpenTelemetryBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder
            .WithTracing(tracing => tracing.AddJustSayingInstrumentation())
            .WithMetrics(metrics => metrics.AddJustSayingInstrumentation());
    }
}
