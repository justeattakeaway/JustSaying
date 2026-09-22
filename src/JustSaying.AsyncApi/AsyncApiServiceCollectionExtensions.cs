using JustSaying.AsyncApi;
using JustSaying.Messaging.Metadata;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// <see cref="IAsyncApiDocumentProvider"/> that generates AsyncAPI 3.0 documents from it.
    /// </summary>
    /// <param name="services">The service collection to add AsyncAPI support to.</param>
    /// <param name="configure">An optional delegate used to configure the <see cref="AsyncApiOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
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
            serviceProvider.GetService<JustSaying.Messaging.MessageSerialization.IMessageBodySerializationFactory>(),
            serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<AsyncApiDocumentGenerator>>()));
        services.TryAddSingleton<IAsyncApiDocumentProvider, AsyncApiDocumentProvider>();

        return services;
    }
}
