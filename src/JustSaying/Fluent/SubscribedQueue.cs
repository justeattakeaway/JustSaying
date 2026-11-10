using JustSaying.Extensions;

namespace JustSaying.Fluent;

/// <summary>
/// The queue a subscription reads from, recorded so that two subscriptions to one queue fail when the
/// bus is built. A queue is identified by its region and name, whether JustSaying owns it (named, in the
/// bus region) or it is addressed by URL or ARN, so the same queue is recognised however it is given.
/// </summary>
internal sealed class SubscribedQueue
{
    private SubscribedQueue(
        string region,
        string accountId,
        string queueName,
        string destination,
        IReadOnlyCollection<Type> messageTypes,
        bool isMultiType,
        IReadOnlyCollection<SubscribedTopic> topics)
    {
        Region = region;
        AccountId = accountId;
        QueueName = queueName;
        Destination = destination;
        MessageTypes = messageTypes;
        IsMultiType = isMultiType;
        Topics = topics;
    }

    /// <summary>
    /// Gets the region the queue is in.
    /// </summary>
    public string Region { get; }

    /// <summary>
    /// Gets the account that owns the queue, when known (it isn't for a queue JustSaying owns).
    /// </summary>
    public string AccountId { get; }

    /// <summary>
    /// Gets the name of the queue.
    /// </summary>
    public string QueueName { get; }

    /// <summary>
    /// Gets how the queue is given to <c>ForQueue</c>, for the suggested fix in an error message.
    /// </summary>
    public string Destination { get; }

    /// <summary>
    /// Gets the message types the subscription reads from the queue.
    /// </summary>
    public IReadOnlyCollection<Type> MessageTypes { get; }

    /// <summary>
    /// Gets whether the subscription is a multi-type queue subscription.
    /// </summary>
    public bool IsMultiType { get; }

    /// <summary>
    /// Gets the topics the subscription subscribes the queue to.
    /// </summary>
    public IReadOnlyCollection<SubscribedTopic> Topics { get; }

    private bool IsOwned => AccountId is null;

    /// <summary>
    /// Creates a <see cref="SubscribedQueue"/> for a queue JustSaying owns, in the bus region.
    /// </summary>
    public static SubscribedQueue Owned(
        string region,
        string queueName,
        IReadOnlyCollection<Type> messageTypes,
        bool isMultiType,
        IReadOnlyCollection<SubscribedTopic> topics)
        => new(region, null, queueName, $"\"{queueName}\"", messageTypes, isMultiType, topics);

    /// <summary>
    /// Creates a <see cref="SubscribedQueue"/> for a pre-existing queue addressed by URL or ARN.
    /// </summary>
    /// <param name="queue">The addressed queue.</param>
    /// <param name="busRegion">The bus region, assumed for a queue URL that doesn't say its region (for example a local emulator's).</param>
    /// <param name="messageTypes">The message types the subscription reads from the queue.</param>
    /// <param name="isMultiType">Whether the subscription is a multi-type queue subscription.</param>
    public static SubscribedQueue Addressed(
        QueueAddressQueue queue,
        string busRegion,
        IReadOnlyCollection<Type> messageTypes,
        bool isMultiType)
    {
        var region = string.Equals(queue.RegionSystemName, "unknown", StringComparison.Ordinal) ? busRegion : queue.RegionSystemName;

        return new(
            region,
            queue.AccountId,
            queue.QueueName,
            $"{nameof(QueueDestination)}.{nameof(QueueDestination.FromUrl)}(\"{queue.Uri.AbsoluteUri}\")",
            messageTypes,
            isMultiType,
            []);
    }

    /// <summary>
    /// Gets whether this and <paramref name="other"/> are the same queue. A queue JustSaying owns is in
    /// the bus's account, which isn't known until startup, so it matches an addressed queue of the same
    /// region and name in any account.
    /// </summary>
    public bool IsSameQueueAs(SubscribedQueue other)
        => string.Equals(QueueName, other.QueueName, StringComparison.Ordinal)
           && string.Equals(Region, other.Region, StringComparison.OrdinalIgnoreCase)
           && (IsOwned || other.IsOwned || string.Equals(AccountId, other.AccountId, StringComparison.Ordinal));

    /// <summary>
    /// Returns why this subscription can't share its queue with <paramref name="existing"/>, or
    /// <see langword="null"/> when it can.
    /// </summary>
    public string GetConflictWith(SubscribedQueue existing)
    {
        if (!IsMultiType && !existing.IsMultiType && MessageTypes.Single() == existing.MessageTypes.Single())
        {
            // The same type read through several topics (or a topic and the queue itself) is safe, as every
            // reader reads the right type. The same type from the same source is the same subscription twice.
            var sameSource = (Topics.Count == 0 && existing.Topics.Count == 0)
                             || Topics.Any(topic => existing.Topics.Any(topic.IsSameTopicAs));

            if (!sameSource)
            {
                return null;
            }

            return $"The queue '{QueueName}' is subscribed to more than once by {Describe(this)}. " +
                   "Remove the duplicate registration.";
        }

        var owned = existing.IsOwned ? existing : IsOwned ? this : existing;
        var allTypes = existing.MessageTypes.Concat(MessageTypes).Distinct().ToList();
        var allTopics = new List<SubscribedTopic>();
        foreach (var topic in existing.Topics.Concat(Topics))
        {
            if (!allTopics.Any(topic.IsSameTopicAs))
            {
                allTopics.Add(topic);
            }
        }

        return $"The queue '{QueueName}' is subscribed to more than once: by {Describe(existing)} and by {Describe(this)}. " +
               "Subscriptions that share a queue compete for its messages, so each would receive the other's and read them as the wrong type. " +
               (allTopics.Count == 0
                   ? "Subscribe to the queue once, handling every type it carries: "
                   : "Subscribe to the queue once, handling every type it carries and subscribing it to every topic: ") +
               $"ForQueue({owned.Destination}, q => q" +
               string.Concat(allTypes.Select(type => $".Handling<{type.ToReadableName()}>()")) +
               string.Concat(allTopics.Select(topic => topic.ToSubscribeCall())) +
               ").";
    }

    private static string Describe(SubscribedQueue subscription)
    {
        if (subscription.IsMultiType)
        {
            return $"a multi-type subscription ({string.Join(", ", subscription.MessageTypes.Select(type => $"'{type.ToReadableName()}'"))})";
        }

        var description = $"'{subscription.MessageTypes.Single().ToReadableName()}'";
        return subscription.Topics.Count == 0
            ? description
            : $"{description} from topic '{subscription.Topics.Single().Name}'";
    }
}

/// <summary>
/// A topic a subscription subscribes its queue to.
/// </summary>
/// <param name="name">The name of the topic.</param>
/// <param name="sourceAccount">The account that owns the topic, for a cross-account subscription.</param>
/// <param name="conventionType">The message type the topic is named after by the naming convention, if it is.</param>
internal sealed class SubscribedTopic(string name, string sourceAccount, Type conventionType)
{
    public string Name { get; } = name;

    public string SourceAccount { get; } = sourceAccount;

    public Type ConventionType { get; } = conventionType;

    public bool IsSameTopicAs(SubscribedTopic other)
        => string.Equals(Name, other.Name, StringComparison.Ordinal)
           && string.Equals(SourceAccount ?? string.Empty, other.SourceAccount ?? string.Empty, StringComparison.Ordinal);

    public string ToSubscribeCall()
        => ConventionType is not null
            ? $".{nameof(MultiTypeQueueSubscriptionBuilder.SubscribeToTopic)}<{ConventionType.ToReadableName()}>()"
            : $".{nameof(MultiTypeQueueSubscriptionBuilder.SubscribeToTopic)}({nameof(TopicDestination)}.{nameof(TopicDestination.Named)}(\"{Name}\"))";
}
