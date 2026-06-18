using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JustSaying.IntegrationTests.Aspire;

/// <summary>
/// A health check that polls floci's localstack-compatible health endpoint so the
/// Aspire test host can wait for the container to be ready before tests run.
/// </summary>
#pragma warning disable CA1001
public sealed class FlociHealthCheck(Uri uri) : IHealthCheck
{
    private readonly HttpClient _client =
        new(new SocketsHttpHandler { ActivityHeadersPropagator = null })
        {
            BaseAddress = uri,
            Timeout = TimeSpan.FromSeconds(1)
        };

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
#pragma warning disable CA2234
            using var response = await _client.GetAsync("_localstack/health", cancellationToken);
#pragma warning restore CA2234
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("floci is healthy.")
                : HealthCheckResult.Unhealthy($"floci health check returned status code: {response.StatusCode}");
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy("floci is unhealthy.", ex);
        }
    }
}
