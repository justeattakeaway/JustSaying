namespace JustSaying.Messaging;

/// <summary>
/// Provides intrinsic, CloudEvents-shaped metadata for a message payload that may or may not derive
/// from <see cref="Models.Message"/>.
/// <para>
/// The default implementation reads the metadata exposed by <see cref="Models.Message"/> and reports
/// its absence for other payload types. Any <em>defaulting</em> or <em>generation</em> — for example
/// minting a CloudEvents <c>id</c> or substituting the current time — is left to the layer that needs
/// it (such as the CloudEvents envelope serializer), so this seam reports only what the message
/// intrinsically carries.
/// </para>
/// </summary>
public interface IMessageMetadataProvider
{
    /// <summary>
    /// Gets a stable identifier for the message (the CloudEvents <c>id</c>), or <see langword="null"/>
    /// if the payload does not expose one.
    /// </summary>
    string GetId(object message);

    /// <summary>
    /// Gets the message's own timestamp (the CloudEvents <c>time</c>), or <see langword="null"/> if the
    /// payload does not expose one.
    /// </summary>
    DateTimeOffset? GetTimestamp(object message);

    /// <summary>
    /// Attempts to get a stable deduplication key for the message, returning <see langword="false"/>
    /// when the payload does not expose one.
    /// </summary>
    bool TryGetDeduplicationKey(object message, out string deduplicationKey);
}
