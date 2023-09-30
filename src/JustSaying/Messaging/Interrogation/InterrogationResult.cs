using Newtonsoft.Json;

namespace JustSaying.Messaging.Interrogation;

/// <summary>
/// This type represents the result of interrogating a bus component.
/// </summary>
[JsonConverter(typeof(InterrogationResultJsonConverter))]
public sealed class InterrogationResult(object data)
{
    /// <summary>
    /// Serialize this to JSON and log it on startup to gain some valuable insights into what's
    /// going on inside JustSaying.
    /// </summary>
    /// <remarks>This property is intentionally untyped, because it is unstructured and subject to change.
    /// It should only be used for diagnostic purposes.</remarks>
    public object Data { get; } = data;

    internal static InterrogationResult Empty { get; } = new InterrogationResult(new {});
}
