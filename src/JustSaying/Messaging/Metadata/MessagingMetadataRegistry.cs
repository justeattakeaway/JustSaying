namespace JustSaying.Messaging.Metadata;

/// <summary>
/// The default <see cref="IMessagingMetadataRegistry"/>. Thread-safe, and deduplicates
/// entries because publication configuration can run for both the publisher and the
/// batch publisher.
/// </summary>
public sealed class MessagingMetadataRegistry : IMessagingMetadataRegistry
{
    private readonly object _syncRoot = new();
    private readonly List<PublicationMetadata> _publications = [];
    private readonly List<SubscriptionMetadata> _subscriptions = [];
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public string Region { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<PublicationMetadata> Publications
    {
        get
        {
            lock (_syncRoot)
            {
                return [.. _publications];
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<SubscriptionMetadata> Subscriptions
    {
        get
        {
            lock (_syncRoot)
            {
                return [.. _subscriptions];
            }
        }
    }

    /// <inheritdoc />
    public void SetRegion(string region)
    {
        lock (_syncRoot)
        {
            Region ??= region;
        }
    }

    /// <inheritdoc />
    public void AddPublication(PublicationMetadata publication)
    {
        if (publication == null) throw new ArgumentNullException(nameof(publication));

        lock (_syncRoot)
        {
            if (_keys.Add(Key("pub", publication.DestinationKind.ToString(), publication.DestinationName, publication.IsDynamic.ToString(), publication.Region, publication.Messages)))
            {
                _publications.Add(publication);
            }
        }
    }

    /// <inheritdoc />
    public void AddSubscription(SubscriptionMetadata subscription)
    {
        if (subscription == null) throw new ArgumentNullException(nameof(subscription));

        lock (_syncRoot)
        {
            if (_keys.Add(Key("sub", subscription.QueueName, subscription.TopicName, subscription.SubscriptionGroupName, subscription.Region, subscription.Messages)))
            {
                _subscriptions.Add(subscription);
            }
        }
    }

    private static string Key(string direction, string first, string second, string third, string region, IReadOnlyList<MessageTypeMetadata> messages)
        => string.Join("|", direction, first, second, third, region, string.Join(",", messages.Select((m) => m.MessageType.AssemblyQualifiedName)));
}
