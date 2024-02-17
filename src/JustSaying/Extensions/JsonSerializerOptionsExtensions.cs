#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JustSaying.Extensions;


internal static class JsonSerializerOptionsExtensions
{
    public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options)
    {
        foreach (var info in options.TypeInfoResolverChain)
        {
            Console.WriteLine(info);
        }

        var typeInfo = options.GetTypeInfo(typeof(T));
        if (typeInfo is not JsonTypeInfo<T> genericTypeInfo)
        {
            throw new JsonException($"Could not find type info for the specified type {typeof(T).Name}");
        }

        return genericTypeInfo;
    }
}
#endif
