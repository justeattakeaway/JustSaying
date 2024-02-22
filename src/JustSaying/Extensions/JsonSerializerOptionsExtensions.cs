#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JustSaying.Extensions;


internal static class JsonSerializerOptionsExtensions
{
    public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options)
    {
        // This is not guarded as we want to throw if the desired type has not been configured for
        var typeInfo = options.GetTypeInfo(typeof(T));
        return (JsonTypeInfo<T>)typeInfo;
    }
}
#endif
