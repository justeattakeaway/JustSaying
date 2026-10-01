namespace JustSaying.Messaging.MessageSerialization;

using System.Text.Encodings.Web;
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
    /// <item><description>Case-insensitive property names when reading JSON.</description></item>
    /// <item><description>Populating get-only collection and object properties when reading JSON.</description></item>
    /// <item><description>Including public fields.</description></item>
    /// <item><description>Escaping only what JSON requires, rather than all non-ASCII and HTML-sensitive characters.</description></item>
    /// <item><description>Using a <see cref="JsonStringEnumConverter"/> for enum serialization (only when dynamic code is supported).</description></item>
    /// </list>
    /// These keep the wire format and binding close to the Newtonsoft.Json defaults used by JustSaying v8.
    /// To keep the same behaviour with a source-generated context, copy these options and set the resolver, for example
    /// <c>new JsonSerializerOptions(DefaultJsonSerializerOptions) { TypeInfoResolver = MyContext.Default }</c>.
    /// </remarks>
    public static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = CreateDefaultJsonSerializerOptions();

    private static JsonSerializerOptions CreateDefaultJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
            IncludeFields = true,

            // Message bodies are never embedded in HTML, so the default encoder's escaping of
            // non-ASCII and HTML-sensitive characters only inflates payloads (2-3x for non-Latin text).
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
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
