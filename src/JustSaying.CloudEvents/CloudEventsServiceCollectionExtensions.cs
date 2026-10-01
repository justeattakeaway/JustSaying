using JustSaying;
using JustSaying.CloudEvents;
using JustSaying.Messaging;
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
    /// used by the CloudEvents publication and subscription registrations
    /// (<c>WithCloudEventTopic&lt;T&gt;</c>, <c>WithCloudEventQueue&lt;T&gt;</c>,
    /// <c>ForCloudEventTopic&lt;T&gt;</c>, <c>HandlingCloudEvent&lt;T&gt;</c>, …). Registrations that do
    /// not opt into CloudEvents are unaffected — the app-wide serialization factory is left alone (unless
    /// <see cref="CloudEventOptions.UseAsDefault"/> is set), so legacy, plain-JSON and CloudEvents
    /// registrations can coexist in one application.
    /// </summary>
    /// <param name="services">The service collection to add CloudEvents support to.</param>
    /// <param name="configure">
    /// An optional delegate used to configure the <see cref="CloudEventOptions"/>. A consume-only
    /// application can omit it entirely and state each message's <c>type</c> at the subscription via
    /// <c>HandlingCloudEvent&lt;T&gt;("...")</c>, since <c>source</c> and the type map are only needed
    /// when publishing.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    public static IServiceCollection AddJustSayingCloudEvents(
        this IServiceCollection services,
        Action<CloudEventOptions> configure = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        var options = new CloudEventOptions();
        configure?.Invoke(options);

        // The data payload uses the app's own serialization factory by default. With UseAsDefault that
        // registration is about to be replaced by CloudEvents itself, so capture it first.
        var appFactory = options.UseAsDefault ? FindSerializationFactory(services) : null;

        services.TryAddSingleton(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<IMessagingConfig>();
            var dataSerializerFactory = options.DataSerializationFactory
                ?? ResolveAppSerializationFactory(serviceProvider, appFactory, options.UseAsDefault);

            var metadataProvider = (config as MessagingConfig)?.MessageMetadataProvider ?? DefaultMessageMetadataProvider.Instance;

            return new CloudEventSerializationFactory(dataSerializerFactory, metadataProvider, options);
        });

        if (options.UseAsDefault)
        {
            // Replace (rather than TryAdd) so this wins whether it runs before or after AddJustSaying's
            // own TryAdd of the System.Text.Json default — the two calls compose in either order.
            services.Replace(ServiceDescriptor.Singleton<IMessageBodySerializationFactory>(
                serviceProvider => serviceProvider.GetRequiredService<CloudEventSerializationFactory>()));
        }

        return services;
    }

    private static ServiceDescriptor FindSerializationFactory(IServiceCollection services)
        => services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IMessageBodySerializationFactory)
#if NET8_0_OR_GREATER
                                                && !descriptor.IsKeyedService
#endif
                                                );

    private static IMessageBodySerializationFactory ResolveAppSerializationFactory(
        IServiceProvider serviceProvider,
        ServiceDescriptor capturedFactory,
        bool useAsDefault)
    {
        // With UseAsDefault the registered factory is CloudEvents itself, so the app's own is the one
        // captured before it was replaced (if there was one yet: AddJustSaying may come later, and then
        // its default is never registered).
        var appFactory = useAsDefault
            ? capturedFactory is null ? null : CreateInstance(capturedFactory, serviceProvider)
            : serviceProvider.GetService<IMessageBodySerializationFactory>();

        return appFactory is null or CloudEventSerializationFactory
            ? new SystemTextJsonSerializationFactory(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)
            : appFactory;
    }

    private static IMessageBodySerializationFactory CreateInstance(ServiceDescriptor descriptor, IServiceProvider serviceProvider)
        => (IMessageBodySerializationFactory)(descriptor.ImplementationInstance
            ?? descriptor.ImplementationFactory?.Invoke(serviceProvider)
            ?? ActivatorUtilities.CreateInstance(serviceProvider, descriptor.ImplementationType));
}
