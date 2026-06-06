using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JustSaying.IntegrationTests.Aspire;

/// <summary>
/// Aspire helpers for running the floci local AWS emulator (https://github.com/floci-io/floci)
/// in a container for the integration tests.
/// </summary>
public static class FlociAspireExtensions
{
    public static IDistributedApplicationBuilder AddFloci(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var floci =
            builder
                .AddContainer("floci", "floci/floci", "latest")
                .WithHttpEndpoint(targetPort: 4566)
                .WithFlociHealthCheck();

        var isRunningInCi = string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.Ordinal);

        if (!isRunningInCi)
        {
            // Reuse a single persistent container across local runs for faster iteration.
            floci
                .WithContainerName("justsaying-floci")
                .WithLifetime(ContainerLifetime.Persistent);
        }

        return builder;
    }

    private static IResourceBuilder<T> WithFlociHealthCheck<T>(this IResourceBuilder<T> builder)
        where T : IResourceWithEndpoints
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpoint = builder.Resource.GetEndpoint("http");
        if (endpoint.Scheme != "http")
        {
            throw new DistributedApplicationException(
                $"Could not create HTTP health check for resource '{builder.Resource.Name}' as the endpoint with name '{endpoint.EndpointName}' and scheme '{endpoint.Scheme}' is not an HTTP endpoint.");
        }

        builder.EnsureEndpointIsAllocated(endpoint);

        Uri baseUri = null;
        builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(builder.Resource, (_, _) =>
        {
            baseUri = new Uri(endpoint.Url, UriKind.Absolute);
            return Task.CompletedTask;
        });

        var healthCheckKey = $"{builder.Resource.Name}_floci_check";

        builder.ApplicationBuilder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
            healthCheckKey,
            _ => baseUri switch
            {
                null => throw new DistributedApplicationException(
                    "The URI for the health check is not set. Ensure that the resource has been allocated before the health check is executed."),
                _ => new FlociHealthCheck(baseUri)
            },
            failureStatus: null,
            tags: null));

        builder.WithHealthCheck(healthCheckKey);

        return builder;
    }

    private static void EnsureEndpointIsAllocated<T>(this IResourceBuilder<T> builder, EndpointReference endpoint)
        where T : IResourceWithEndpoints
    {
        var endpointName = endpoint.EndpointName;

        builder.OnResourceEndpointsAllocated((_, _, _) =>
            endpoint.Exists switch
            {
                true => Task.CompletedTask,
                false => throw new DistributedApplicationException(
                    $"The endpoint '{endpointName}' does not exist on the resource '{builder.Resource.Name}'.")
            });
    }
}
