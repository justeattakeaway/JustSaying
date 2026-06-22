using JustSaying.Models;

namespace JustSaying.Messaging;

/// <summary>
/// Extracts identity information from a message payload.
/// <para>
/// JustSaying historically required every message to derive from <see cref="Message"/>, which
/// exposes a stable <see cref="Message.Id"/> and <see cref="Message.UniqueKey"/>. To support
/// arbitrary, unconstrained payloads, the pipeline now flows messages as <see cref="object"/> and
/// uses this helper to obtain identity where one is needed, degrading gracefully when the payload
/// does not derive from <see cref="Message"/>.
/// </para>
/// </summary>
internal static class MessageIdentity
{
    /// <summary>
    /// Gets a stable identifier for a message for telemetry and logging purposes, or
    /// <see langword="null"/> if the payload does not expose one.
    /// </summary>
    public static string GetId(object message)
        => message is Message typed ? typed.Id.ToString() : null;

    /// <summary>
    /// Gets a unique key for a message, suitable for use as a batch request entry identifier or for
    /// deduplication. Falls back to a fresh <see cref="Guid"/> for payloads that do not derive from
    /// <see cref="Message"/>, which guarantees uniqueness within a batch but is not stable across
    /// publishes.
    /// </summary>
    public static string GetUniqueKey(object message)
        => message is Message typed ? typed.UniqueKey() : Guid.NewGuid().ToString();

    /// <summary>
    /// Attempts to get a stable unique key for a message, returning <see langword="false"/> when the
    /// payload does not expose one. Used by features such as exactly-once handling that cannot
    /// function without a caller-provided identity.
    /// </summary>
    public static bool TryGetUniqueKey(object message, out string uniqueKey)
    {
        if (message is Message typed)
        {
            uniqueKey = typed.UniqueKey();
            return true;
        }

        uniqueKey = null;
        return false;
    }
}
