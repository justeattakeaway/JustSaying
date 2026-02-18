using JustSaying.Messaging.Monitoring;
using OpenTelemetry.Metrics;

namespace JustSaying.Extensions.OpenTelemetry;

/// <summary>
/// Extension methods for configuring JustSaying metrics with OpenTelemetry.
/// </summary>
public static class MeterProviderBuilderExtensions
{
    /// <summary>
    /// Adds JustSaying instrumentation to the <see cref="MeterProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="MeterProviderBuilder"/> to configure.</param>
    /// <returns>The <see cref="MeterProviderBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static MeterProviderBuilder AddJustSayingInstrumentation(this MeterProviderBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddMeter(JustSayingDiagnostics.MeterName);
    }
}
