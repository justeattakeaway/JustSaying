using System.Text.Json;
using JustSaying;
using JustSaying.CloudEvents;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring CloudEvents support on an <see cref="IServiceCollection"/>.
/// </summary>
public static class CloudEventsServiceCollectionExtensions
{
    /// <summary>
    /// Configures JustSaying to serialize and deserialize messages as structured-mode CloudEvents.
    /// Registers a CloudEvents <see cref="IMessageBodySerializationFactory"/> (wrapping a
    /// System.Text.Json factory for the <c>data</c> payload) so it is used by <c>AddJustSaying</c>.
    /// </summary>
    /// <param name="services">The service collection to add CloudEvents support to.</param>
    /// <param name="configure">A delegate used to configure the <see cref="CloudEventOptions"/>.</param>
    /// <param name="dataSerializerOptions">
    /// Optional <see cref="JsonSerializerOptions"/> for the <c>data</c> payload. Supply one with a
    /// source-generated <c>TypeInfoResolver</c> to remain Native AOT-compatible; when
    /// <see langword="null"/>, reflection-based defaults are used.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    public static IServiceCollection AddJustSayingCloudEvents(
        this IServiceCollection services,
        Action<CloudEventOptions> configure,
        JsonSerializerOptions dataSerializerOptions = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        var options = new CloudEventOptions();
        configure(options);

        services.TryAddSingleton<IMessageBodySerializationFactory>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<IMessagingConfig>();
            var dataSerializerFactory = new SystemTextJsonSerializationFactory(
                dataSerializerOptions ?? SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);

            return new CloudEventSerializationFactory(dataSerializerFactory, config.MessageMetadataProvider, options);
        });

        return services;
    }
}
