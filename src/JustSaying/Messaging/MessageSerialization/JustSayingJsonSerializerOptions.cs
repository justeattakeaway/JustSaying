#if NET8_0_OR_GREATER
using System.Text.Json;

namespace JustSaying.Messaging.MessageSerialization;

public class JustSayingJsonSerializerOptions
{
    public JsonSerializerOptions SerializerOptions { get; } = new();
}
#endif
