using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Fluent;

/// <summary>
/// Configures the SNS topic JustSaying creates for a publication registered against a
/// <see cref="TopicDestination"/> that JustSaying owns (<see cref="TopicDestination.Named(string, Action{TopicInfrastructure})"/>
/// or <see cref="TopicDestination.ByConvention(Action{TopicInfrastructure})"/>). A pre-existing topic
/// (<see cref="TopicDestination.FromArn(string)"/>) is never created, so it offers no such configuration.
/// This class cannot be inherited.
/// </summary>
public sealed class TopicInfrastructure
{
    internal TopicInfrastructure()
    { }

    internal Dictionary<string, string> Tags { get; } = new(StringComparer.Ordinal);

    internal ServerSideEncryption Encryption { get; private set; }

    /// <summary>
    /// Creates a tag with no value that will be assigned to the SNS topic.
    /// </summary>
    /// <param name="key">The key for the tag.</param>
    /// <returns>The current <see cref="TopicInfrastructure"/>.</returns>
    /// <remarks>Tag keys are case-sensitive. A new tag with a key identical to that of an existing one will overwrite it.</remarks>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see langword="null"/> or whitespace.</exception>
    public TopicInfrastructure WithTag(string key) => WithTag(key, null);

    /// <summary>
    /// Creates a tag with a value that will be assigned to the SNS topic.
    /// </summary>
    /// <param name="key">The key for the tag.</param>
    /// <param name="value">The value associated with this tag.</param>
    /// <returns>The current <see cref="TopicInfrastructure"/>.</returns>
    /// <remarks>Tag keys are case-sensitive. A new tag with a key identical to that of an existing one will overwrite it.</remarks>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see langword="null"/> or whitespace.</exception>
    public TopicInfrastructure WithTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A topic tag key cannot be null or only whitespace", nameof(key));
        }

        Tags[key] = value ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Configures server-side encryption for the topic.
    /// </summary>
    /// <param name="encryption">The server-side encryption to apply when the topic is created.</param>
    /// <returns>The current <see cref="TopicInfrastructure"/>.</returns>
    public TopicInfrastructure WithEncryption(ServerSideEncryption encryption)
    {
        Encryption = encryption;
        return this;
    }

    /// <summary>
    /// Configures server-side encryption for the topic with the specified KMS master key.
    /// </summary>
    /// <param name="kmsMasterKeyId">The id of the KMS master key to encrypt the topic with.</param>
    /// <returns>The current <see cref="TopicInfrastructure"/>.</returns>
    public TopicInfrastructure WithEncryption(string kmsMasterKeyId)
        => WithEncryption(new ServerSideEncryption { KmsMasterKeyId = kmsMasterKeyId });
}
