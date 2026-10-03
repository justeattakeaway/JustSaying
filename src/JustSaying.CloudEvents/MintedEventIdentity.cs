using System.Runtime.CompilerServices;
using JustSaying.Messaging;

namespace JustSaying.CloudEvents;

/// <summary>
/// The CloudEvents <c>id</c> and <c>time</c> minted for a published message that carries neither.
/// </summary>
/// <remarks>
/// The bus serializes a message again for every publish attempt, so an <c>id</c> minted per call to
/// <c>Serialize</c> would differ between the retries of one <c>PublishAsync</c> call, making a
/// duplicate delivery look like a different event to consumers. They are minted once per message
/// instance instead (as a <see cref="Models.Message"/>'s <c>Id</c> is fixed when it is created), and
/// held only for as long as the instance itself is alive.
/// </remarks>
internal sealed class MintedEventIdentity
{
    private static readonly ConditionalWeakTable<object, MintedEventIdentity> Minted = new();

    private MintedEventIdentity()
    {
    }

    public string Id { get; } = Guid.NewGuid().ToString();

    public DateTimeOffset Time { get; } = DateTimeOffset.UtcNow;

    public static MintedEventIdentity For(object message)
        => Minted.GetValue(message, static _ => new MintedEventIdentity());

    /// <summary>
    /// Wraps a metadata provider so that a message without its own id or timestamp gets the minted one,
    /// for the serializer that publishes a bare payload as a CloudEvent.
    /// </summary>
    public static IMessageMetadataProvider Fallback(IMessageMetadataProvider inner) => new FallbackMetadataProvider(inner);

    private sealed class FallbackMetadataProvider(IMessageMetadataProvider inner) : IMessageMetadataProvider
    {
        public string GetId(object message)
            => inner.GetId(message) is { Length: > 0 } id ? id : For(message).Id;

        public DateTimeOffset? GetTimestamp(object message)
            => inner.GetTimestamp(message) ?? For(message).Time;

        public bool TryGetDeduplicationKey(object message, out string deduplicationKey)
            => inner.TryGetDeduplicationKey(message, out deduplicationKey);
    }
}
