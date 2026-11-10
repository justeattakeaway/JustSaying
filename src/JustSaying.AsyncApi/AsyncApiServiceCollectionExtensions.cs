using JustSaying.AsyncApi;
using JustSaying.Messaging.Metadata;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring AsyncAPI document generation on an <see cref="IServiceCollection"/>.
/// </summary>
public static class AsyncApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds AsyncAPI document generation for the JustSaying messaging bus. Registers an
    /// <see cref="IMessagingMetadataRegistry"/>, which the fluent builders populate with
    /// publication and subscription metadata as the bus is configured, and an
    /// <see cref="IAsyncApiDocumentProvider"/> that generates AsyncAPI 3.1 documents from it.
    /// </summary>
    /// <param name="services">The service collection to add AsyncAPI support to.</param>
    /// <param name="configure">An optional delegate used to configure the <see cref="AsyncApiOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Documents are written with ByteBard.AsyncAPI.NET, which is not annotated for trimming or
    /// Native AOT, so this package is not marked trim- or AOT-compatible: publishing a trimmed or
    /// Native AOT application that references it produces trim/AOT warnings (IL2104, IL3053).
    /// </para>
    /// <para>
    /// A payload type that refers to itself, directly or through other types, is described once in
    /// the document's <c>components/schemas</c> and referenced with <c>$ref</c> wherever it appears,
    /// including within itself; JSON Schema has no other way to describe recursion. The document is
    /// valid AsyncAPI, but the AsyncAPI CLI's <c>asyncapi bundle</c> command fails on it with
    /// "Circular $ref pointer found", because it dereferences every <c>$ref</c> and has no option
    /// not to. Generated documents only reference themselves, so they don't need bundling; to bundle
    /// one with other files, use a bundler that keeps references, such as <c>redocly bundle</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddJustSayingAsyncApi(
        this IServiceCollection services,
        Action<AsyncApiOptions> configure = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        var options = new AsyncApiOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IMessagingMetadataRegistry, MessagingMetadataRegistry>();
        services.TryAddSingleton((serviceProvider) => new AsyncApiDocumentGenerator(
            serviceProvider.GetRequiredService<IMessagingMetadataRegistry>(),
            serviceProvider.GetRequiredService<AsyncApiOptions>(),
            serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<AsyncApiDocumentGenerator>>())
        {
            ApplicationName = serviceProvider.GetService<IHostEnvironment>()?.ApplicationName,
        });
        services.TryAddSingleton<IAsyncApiDocumentProvider, AsyncApiDocumentProvider>();

        return services;
    }
}
