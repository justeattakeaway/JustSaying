namespace JustSaying.Extensions.OpenTelemetry.Tests;

/// <summary>
/// Prevents the Tracing collection from running in parallel with any other collection.
/// JustSayingDiagnostics.ActivitySource is process-global, so tracing tests must be isolated.
/// </summary>
[CollectionDefinition("Tracing", DisableParallelization = true)]
public class TracingCollectionDefinition { }

/// <summary>
/// Prevents the Metrics collection from running in parallel with any other collection.
/// JustSayingDiagnostics.Meter instruments are process-global, so metrics tests must be isolated.
/// </summary>
[CollectionDefinition("Metrics", DisableParallelization = true)]
public class MetricsCollectionDefinition { }
