using System.Globalization;
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
/// <para>
/// Identity is read through the bus's <see cref="IMessageMetadataProvider"/>
/// (<see cref="JustSayingBus.MessageMetadataProvider"/>), so the same provider is used everywhere a
/// message identity is surfaced.
/// </para>
/// </summary>
internal static class MessageIdentity
{
    /// <summary>
    /// Gets a stable identifier for a message for telemetry and logging purposes, or
    /// <see langword="null"/> if the payload does not expose one.
    /// </summary>
    public static string GetId(object message, IMessageMetadataProvider metadataProvider)
        => (metadataProvider ?? DefaultMessageMetadataProvider.Instance).GetId(message);

    /// <summary>
    /// Gets the batch request entry identifier for the message at <paramref name="index"/> in a batch
    /// request. SNS and SQS only require entry ids to match <c>[A-Za-z0-9_-]{1,80}</c> and be unique
    /// within the request, so the position is used rather than anything derived from the message,
    /// such as a deduplication key, which may repeat or contain other characters.
    /// </summary>
    public static string GetBatchEntryId(int index)
        => index.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the message a batch result entry refers to, from the entry id created by
    /// <see cref="GetBatchEntryId(int)"/>, or <see langword="null"/> if the id isn't one of ours.
    /// </summary>
    public static object GetBatchEntryMessage(IReadOnlyList<object> messages, string entryId)
        => int.TryParse(entryId, NumberStyles.None, CultureInfo.InvariantCulture, out int index) && index < messages.Count
            ? messages[index]
            : null;

    /// <summary>
    /// Gets the identifier to report for a batch result entry: the message's own id when it has one,
    /// otherwise the entry id (its position in <paramref name="messages"/>).
    /// </summary>
    public static string GetBatchEntryMessageId(IReadOnlyList<object> messages, string entryId, IMessageMetadataProvider metadataProvider)
        => GetBatchEntryMessage(messages, entryId) is { } message
            ? GetId(message, metadataProvider) ?? entryId
            : entryId;
}
