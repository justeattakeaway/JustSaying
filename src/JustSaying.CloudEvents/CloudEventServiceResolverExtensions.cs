using JustSaying.Fluent;

namespace JustSaying.CloudEvents;

/// <summary>
/// Resolves the <see cref="CloudEventSerializationFactory"/> from the bus's service resolver for the
/// CloudEvents registration extensions, with a setup-pointing error when it is not registered.
/// </summary>
internal static class CloudEventServiceResolverExtensions
{
    public static CloudEventSerializationFactory ResolveCloudEventSerializationFactory(this IServiceResolver serviceResolver)
        => serviceResolver?.ResolveOptionalService<CloudEventSerializationFactory>()
           ?? throw new InvalidOperationException(
               "A CloudEvents publication or subscription is registered, but CloudEvents support is not; call AddJustSayingCloudEvents(...) on the service collection.");
}
