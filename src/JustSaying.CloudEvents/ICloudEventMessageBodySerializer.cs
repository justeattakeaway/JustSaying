using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.CloudEvents;

/// <summary>
/// Implemented by the CloudEvents serializers to describe the structured-mode envelope they write,
/// so that tooling (such as AsyncAPI document generation) can document a registration's wire format
/// from the serializer it actually uses rather than from application-wide configuration.
/// </summary>
/// <remarks>
/// The <see cref="IMessageBodyFormat.ContentType"/> of the envelope is
/// <c>application/cloudevents+json</c>; <see cref="DataContentType"/> is that of its <c>data</c>.
/// </remarks>
public interface ICloudEventMessageBodySerializer : IMessageBodyFormat
{
    /// <summary>
    /// Gets the CloudEvents <c>type</c> written to the envelope, or <see langword="null"/> when the
    /// serializer is only used to consume and none is configured.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Gets the default CloudEvents <c>source</c> written to the envelope, or <see langword="null"/>
    /// when none is configured (a published <see cref="CloudEvent{T}"/> may still carry its own).
    /// </summary>
    Uri Source { get; }

    /// <summary>
    /// Gets the CloudEvents <c>datacontenttype</c> written to the envelope.
    /// </summary>
    string DataContentType { get; }

    /// <summary>
    /// Gets the CLR type of the <c>data</c> payload.
    /// </summary>
    Type DataType { get; }

    /// <summary>
    /// Gets the format of the envelope's <c>data</c> member, as described by the serializer that
    /// produces it, or <see langword="null"/> when that serializer does not describe its format.
    /// </summary>
    IMessageBodyFormat DataFormat { get; }
}
