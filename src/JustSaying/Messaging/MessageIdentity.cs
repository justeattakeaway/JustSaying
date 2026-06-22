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
    /// Gets an identifier for a message suitable for use as a batch request entry identifier, which
    /// only needs to be unique <em>within a single batch request</em>. Falls back to a fresh
    /// <see cref="Guid"/> for payloads that do not derive from <see cref="Message"/>.
    /// <para>
    /// This is deliberately <em>not</em> suitable for deduplication: the fallback is not stable
    /// across publishes. Features that need a stable key (such as exactly-once handling) must obtain
    /// it explicitly rather than calling this method.
    /// </para>
    /// </summary>
    public static string GetBatchEntryId(object message)
        => message is Message typed ? typed.UniqueKey() : Guid.NewGuid().ToString();
}
