#nullable enable
using System.Text.Json;

namespace JustSaying.Extensions;

internal static class JsonElementExtensions
{
#if NET8_0_OR_GREATER
    public static bool TryGetStringProperty(this JsonElement element, string key, [NotNullWhen(true)] out string? value)
#else
    public static bool TryGetStringProperty(this JsonElement element, string key, out string? value)
#endif
    {
        value = null;
        if (element.TryGetProperty(key, out var property)
            && property.ValueKind is JsonValueKind.String or JsonValueKind.Null
            && property.GetString() is {} propertyValue)
        {
            value = propertyValue;
            return true;
        }

        return false;
    }
}
