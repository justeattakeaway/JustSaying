using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Fluent;

/// <summary>
/// Configures the SQS queue JustSaying creates for a registration against a <see cref="QueueDestination"/>
/// that JustSaying owns (<see cref="QueueDestination.Named(string, Action{QueueInfrastructure})"/> or
/// <see cref="QueueDestination.ByConvention(Action{QueueInfrastructure})"/>). A pre-existing queue
/// (<see cref="QueueDestination.FromUri(Uri, string)"/> and friends) is never created, so it offers no such
/// configuration. This class cannot be inherited.
/// </summary>
public sealed class QueueInfrastructure
{
    internal QueueInfrastructure()
    { }

    internal Dictionary<string, string> Tags { get; } = new(StringComparer.Ordinal);

    internal TimeSpan? MessageRetention { get; private set; }

    internal TimeSpan? VisibilityTimeout { get; private set; }

    internal TimeSpan? DeliveryDelay { get; private set; }

    internal int? RetriesBeforeErrorQueue { get; private set; }

    internal bool ErrorQueueOptOut { get; private set; }

    internal TimeSpan? ErrorQueueRetention { get; private set; }

    internal ServerSideEncryption Encryption { get; private set; }

    /// <summary>
    /// Creates a tag with no value that will be assigned to the SQS queue.
    /// </summary>
    /// <param name="key">The key for the tag.</param>
    /// <returns>The current <see cref="QueueInfrastructure"/>.</returns>
    /// <remarks>Tag keys are case-sensitive. A new tag with a key identical to that of an existing one will overwrite it.</remarks>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see langword="null"/> or whitespace.</exception>
    public QueueInfrastructure WithTag(string key) => WithTag(key, null);

    /// <summary>
    /// Creates a tag with a value that will be assigned to the SQS queue.
    /// </summary>
    /// <param name="key">The key for the tag.</param>
    /// <param name="value">The value associated with this tag.</param>
    /// <returns>The current <see cref="QueueInfrastructure"/>.</returns>
    /// <remarks>Tag keys are case-sensitive. A new tag with a key identical to that of an existing one will overwrite it.</remarks>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see langword="null"/> or whitespace.</exception>
    public QueueInfrastructure WithTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A queue tag key cannot be null or only whitespace", nameof(key));
        }

        Tags[key] = value ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Configures how long messages are retained on the queue.
    /// </summary>
    /// <param name="retention">The message retention period.</param>
    /// <returns>The current <see cref="QueueInfrastructure"/>.</returns>
    public QueueInfrastructure WithMessageRetention(TimeSpan retention)
    {
        MessageRetention = retention;
        return this;
    }

    /// <summary>
    /// Configures the visibility timeout for messages being processed.
    /// </summary>
    /// <param name="timeout">The visibility timeout.</param>
    /// <returns>The current <see cref="QueueInfrastructure"/>.</returns>
    public QueueInfrastructure WithVisibilityTimeout(TimeSpan timeout)
    {
        VisibilityTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Configures the delay before delivering newly published messages.
    /// </summary>
    /// <param name="delay">The delivery delay.</param>
    /// <returns>The current <see cref="QueueInfrastructure"/>.</returns>
    public QueueInfrastructure WithDeliveryDelay(TimeSpan delay)
    {
        DeliveryDelay = delay;
        return this;
    }

    /// <summary>
    /// Configures how many times a message is received before being moved to the error queue.
    /// </summary>
    /// <param name="retryCount">The number of receives before a message is moved to the error queue.</param>
    /// <returns>The current <see cref="QueueInfrastructure"/>.</returns>
    public QueueInfrastructure WithRetriesBeforeErrorQueue(int retryCount)
    {
        RetriesBeforeErrorQueue = retryCount;
        return this;
    }

    /// <summary>
    /// Opts out of creating an error queue alongside this queue.
    /// </summary>
    /// <returns>The current <see cref="QueueInfrastructure"/>.</returns>
    public QueueInfrastructure WithNoErrorQueue()
    {
        ErrorQueueOptOut = true;
        return this;
    }

    /// <summary>
    /// Configures how long messages are retained on the error queue.
    /// </summary>
    /// <param name="retention">The error queue's message retention period.</param>
    /// <returns>The current <see cref="QueueInfrastructure"/>.</returns>
    public QueueInfrastructure WithErrorQueueRetention(TimeSpan retention)
    {
        ErrorQueueRetention = retention;
        return this;
    }

    /// <summary>
    /// Configures server-side encryption for the queue.
    /// </summary>
    /// <param name="encryption">The server-side encryption to apply when the queue is created.</param>
    /// <returns>The current <see cref="QueueInfrastructure"/>.</returns>
    public QueueInfrastructure WithEncryption(ServerSideEncryption encryption)
    {
        Encryption = encryption;
        return this;
    }

    /// <summary>
    /// Configures server-side encryption for the queue with the specified KMS master key.
    /// </summary>
    /// <param name="kmsMasterKeyId">The id of the KMS master key to encrypt the queue with.</param>
    /// <returns>The current <see cref="QueueInfrastructure"/>.</returns>
    public QueueInfrastructure WithEncryption(string kmsMasterKeyId)
        => WithEncryption(new ServerSideEncryption { KmsMasterKeyId = kmsMasterKeyId });

    /// <summary>
    /// Applies the configured settings onto an <see cref="SqsBasicConfiguration"/>, leaving its
    /// defaults in place for anything not configured here.
    /// </summary>
    internal void Apply(SqsBasicConfiguration configuration)
    {
        if (MessageRetention is { } retention) configuration.MessageRetention = retention;
        if (VisibilityTimeout is { } visibility) configuration.VisibilityTimeout = visibility;
        if (DeliveryDelay is { } delay) configuration.DeliveryDelay = delay;
        if (RetriesBeforeErrorQueue is { } retries) configuration.RetryCountBeforeSendingToErrorQueue = retries;
        if (ErrorQueueRetention is { } errorRetention) configuration.ErrorQueueRetentionPeriod = errorRetention;
        if (ErrorQueueOptOut) configuration.ErrorQueueOptOut = true;
        if (Encryption is not null) configuration.ServerSideEncryption = Encryption;
    }
}
