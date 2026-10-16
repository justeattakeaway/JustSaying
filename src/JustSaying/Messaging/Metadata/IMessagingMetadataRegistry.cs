namespace JustSaying.Messaging.Metadata;

/// <summary>
/// A read-only view of the publications and subscriptions configured on the messaging bus.
/// </summary>
/// <remarks>
/// The registry is populated by the fluent builders as they are configured, which happens
/// before the bus is started and before any AWS infrastructure is provisioned. It is only
/// populated when a <see cref="MessagingMetadataRegistry"/> is registered with the service
/// resolver as this interface, for example by a documentation package such as AsyncAPI support.
/// This interface is not intended to be implemented outside JustSaying: the builders only
/// populate a <see cref="MessagingMetadataRegistry"/>.
/// </remarks>
public interface IMessagingMetadataRegistry
{
    /// <summary>
    /// Gets the AWS region the bus is configured for, or <see langword="null"/> if not yet captured.
    /// </summary>
    string Region { get; }

    /// <summary>
    /// Gets the publications captured by the registry.
    /// </summary>
    IReadOnlyCollection<PublicationMetadata> Publications { get; }

    /// <summary>
    /// Gets the subscriptions captured by the registry.
    /// </summary>
    IReadOnlyCollection<SubscriptionMetadata> Subscriptions { get; }
}
