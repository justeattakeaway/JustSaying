namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Implemented by a message body serializer to describe the format of the bodies it writes, so that
/// tooling (such as AsyncAPI document generation) can document a registration's wire format from the
/// serializer it actually uses.
/// </summary>
/// <remarks>
/// <para>
/// A serializer whose format is produced by System.Text.Json implements the more specific
/// <see cref="ISystemTextJsonMessageBodySerializer"/>, from which a payload schema can be derived. A
/// serializer that wraps another (for example to add encryption or logging) and writes the same format
/// can implement the same interface as the serializer it wraps and forward its members.
/// </para>
/// <para>
/// A serializer that implements none of these is documented without a content type or payload schema.
/// </para>
/// </remarks>
public interface IMessageBodyFormat
{
    /// <summary>
    /// Gets the media type of the message bodies written, for example <c>application/json</c>.
    /// </summary>
    string ContentType { get; }
}
