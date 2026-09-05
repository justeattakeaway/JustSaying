namespace JustSaying.Messaging.MessageSerialization;

using System.Text.Json;
using System.Text.Json.Serialization;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

public static class SystemTextJsonMessageBodySerializer
{
    /// <summary>
    /// Gets the default JSON serializer options used by derived serializers.
    /// </summary>
    /// <remarks>
    /// These options include:
    /// <list type="bullet">
    /// <item><description>Ignoring null values when writing JSON.</description></item>
    /// <item><description>Using a <see cref="JsonStringEnumConverter"/> for enum serialization (only when dynamic code is supported).</description></item>
    /// </list>
    /// </remarks>
    public static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = CreateDefaultJsonSerializerOptions();

    private static JsonSerializerOptions CreateDefaultJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

#if NET8_0_OR_GREATER
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
#pragma warning disable IL3050
            options.Converters.Add(new JsonStringEnumConverter());
#pragma warning restore IL3050
        }
#else
        options.Converters.Add(new JsonStringEnumConverter());
#endif

        return options;
    }
}
