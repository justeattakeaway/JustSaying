namespace JustSaying.Messaging.Metadata;

/// <summary>
/// A registry of the publications and subscriptions configured on the messaging bus.
/// </summary>
/// <remarks>
/// The registry is populated by the fluent builders as they are configured, which happens
/// before the bus is started and before any AWS infrastructure is provisioned. It is only
/// populated when an implementation is registered with the service resolver, for example
/// by a documentation package such as AsyncAPI support. Because publisher and subscriber
/// configuration can each run more than once, implementations must deduplicate entries.
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

    /// <summary>
    /// Records the AWS region the bus is configured for.
    /// </summary>
    /// <param name="region">The AWS region system name.</param>
    void SetRegion(string region);

    /// <summary>
    /// Records a publication.
    /// </summary>
    /// <param name="publication">The publication metadata to record.</param>
    void AddPublication(PublicationMetadata publication);

    /// <summary>
    /// Records a subscription.
    /// </summary>
    /// <param name="subscription">The subscription metadata to record.</param>
    void AddSubscription(SubscriptionMetadata subscription);
}
