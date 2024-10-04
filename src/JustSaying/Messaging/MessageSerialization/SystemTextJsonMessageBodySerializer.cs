namespace JustSaying.Messaging.MessageSerialization;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class SystemTextJsonMessageBodySerializer
{
    /// <summary>
    /// Gets the default JSON serializer options used by derived serializers.
    /// </summary>
    /// <remarks>
    /// These options include:
    /// <list type="bullet">
    /// <item><description>Ignoring null values when writing JSON.</description></item>
    /// <item><description>Using a <see cref="JsonStringEnumConverter"/> for enum serialization.</description></item>
    /// </list>
    /// </remarks>
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };
}
