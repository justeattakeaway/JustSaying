using JustSaying.Models;

namespace JustSaying.Messaging;

/// <summary>
/// The default <see cref="IMessageMetadataProvider"/>. Reads the metadata exposed by
/// <see cref="Message"/> and reports its absence for payloads that do not derive from it.
/// </summary>
internal sealed class DefaultMessageMetadataProvider : IMessageMetadataProvider
{
    public static readonly DefaultMessageMetadataProvider Instance = new();

    public string GetId(object message)
        => message is Message typed ? typed.Id.ToString() : null;

    public DateTimeOffset? GetTimestamp(object message)
        => message is Message typed ? new DateTimeOffset(DateTime.SpecifyKind(typed.TimeStamp, DateTimeKind.Utc)) : null;

    public bool TryGetDeduplicationKey(object message, out string deduplicationKey)
    {
        if (message is Message typed)
        {
            deduplicationKey = typed.UniqueKey();
            return true;
        }

        deduplicationKey = null;
        return false;
    }
}
