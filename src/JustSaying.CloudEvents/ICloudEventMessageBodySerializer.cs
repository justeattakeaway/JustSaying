namespace JustSaying.CloudEvents;

/// <summary>
/// Implemented by the CloudEvents serializers to describe the structured-mode envelope they write,
/// so that tooling (such as AsyncAPI document generation) can document a registration's wire format
/// from the serializer it actually uses rather than from application-wide configuration.
/// </summary>
public interface ICloudEventMessageBodySerializer
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
    /// Gets the serializer — an <c>IMessageBodySerializer&lt;T&gt;</c> for <see cref="DataType"/> —
    /// that produces the envelope's <c>data</c> member.
    /// </summary>
    object DataSerializer { get; }
}
