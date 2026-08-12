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
    /// Adds CloudEvents support to JustSaying, registering the <see cref="CloudEventSerializationFactory"/>
    /// (wrapping a System.Text.Json factory for the <c>data</c> payload) used by the CloudEvents
    /// publication and subscription registrations (<c>WithCloudEventTopic&lt;T&gt;</c>,
    /// <c>WithCloudEventQueue&lt;T&gt;</c>, <c>HandlingCloudEvent&lt;T&gt;</c>,
    /// <c>HandlingCloudEventData&lt;T&gt;</c>). Registrations that do
    /// not opt into CloudEvents are unaffected — the app-wide serialization factory is left alone, so
    /// legacy, plain-JSON and CloudEvents registrations can coexist in one application.
    /// </summary>
    /// <param name="services">The service collection to add CloudEvents support to.</param>
    /// <param name="configure">
    /// An optional delegate used to configure the <see cref="CloudEventOptions"/>. A consume-only
    /// application can omit it entirely and state each message's <c>type</c> at the subscription via
    /// <c>HandlingCloudEvent&lt;T&gt;("...")</c>, since <c>source</c> and the type map are only needed
    /// when publishing.
    /// </param>
    /// <param name="dataSerializerOptions">
    /// Optional <see cref="JsonSerializerOptions"/> for the <c>data</c> payload. Supply one with a
    /// source-generated <c>TypeInfoResolver</c> to remain Native AOT-compatible; when
    /// <see langword="null"/>, reflection-based defaults are used.
    /// </param>
    /// <param name="useAsDefault">
    /// When <see langword="true"/>, additionally makes CloudEvents the app-wide default serialization
    /// format, so every plain registration (<c>WithTopic&lt;T&gt;</c>, <c>ForQueue&lt;T&gt;</c>, …)
    /// speaks CloudEvents too — for an all-CloudEvents application. Every published type must then have
    /// a <c>type</c> mapped via <see cref="CloudEventOptions.MapType{TMessage}"/> (an
    /// unmapped type fails at startup rather than silently publishing plain JSON). The default is
    /// <see langword="false"/>: non-CloudEvents registrations keep the app-wide default (System.Text.Json
    /// unless configured otherwise).
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    public static IServiceCollection AddJustSayingCloudEvents(
        this IServiceCollection services,
        Action<CloudEventOptions> configure = null,
        JsonSerializerOptions dataSerializerOptions = null,
        bool useAsDefault = false)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        var options = new CloudEventOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);

        services.TryAddSingleton(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<IMessagingConfig>();
            var dataSerializerFactory = new SystemTextJsonSerializationFactory(
                dataSerializerOptions ?? SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);

            return new CloudEventSerializationFactory(dataSerializerFactory, config.MessageMetadataProvider, options);
        });

        if (useAsDefault)
        {
            // Replace (rather than TryAdd) so this wins whether it runs before or after AddJustSaying's
            // own TryAdd of the System.Text.Json default — the two calls compose in either order.
            services.Replace(ServiceDescriptor.Singleton<IMessageBodySerializationFactory>(
                serviceProvider => serviceProvider.GetRequiredService<CloudEventSerializationFactory>()));
        }

        return services;
    }
}
