#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JustSaying.Extensions;

internal static class JsonSerializerOptionsExtensions
{
    public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options)
    {
        // This throws a NotSupportedException if type information is not available for a given type,
        // or an ArgumentException if the type is not valid for serialization (void, pointer types, and similar).
        // We don't guard this to allow the default exception to be propagated.
        var typeInfo = options.GetTypeInfo(typeof(T));
        return (JsonTypeInfo<T>)typeInfo;
    }
}
#endif
