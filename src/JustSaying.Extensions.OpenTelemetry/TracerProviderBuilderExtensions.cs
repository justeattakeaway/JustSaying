using JustSaying.Messaging.Monitoring;
using OpenTelemetry.Trace;

namespace JustSaying.Extensions.OpenTelemetry;

/// <summary>
/// Extension methods for configuring JustSaying tracing with OpenTelemetry.
/// </summary>
public static class TracerProviderBuilderExtensions
{
    /// <summary>
    /// Adds JustSaying instrumentation to the <see cref="TracerProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to configure.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddJustSayingInstrumentation(this TracerProviderBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder
            .AddSource(JustSayingDiagnostics.ActivitySourceName)
            .AddSource("JustSaying.MessageHandler")
            .AddSource("JustSaying.MessagePublisher");
    }
}
