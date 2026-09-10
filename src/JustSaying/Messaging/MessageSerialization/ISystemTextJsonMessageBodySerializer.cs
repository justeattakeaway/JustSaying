using System.Text.Json;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Implemented by a message body serializer whose wire format is produced by System.Text.Json, exposing
/// the <see cref="JsonSerializerOptions"/> it serializes with so that tooling (such as AsyncAPI document
/// generation) can derive the payload's JSON schema from the same options that shape the wire.
/// </summary>
public interface ISystemTextJsonMessageBodySerializer
{
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> message bodies are serialized with.
    /// </summary>
    JsonSerializerOptions SerializerOptions { get; }
}
