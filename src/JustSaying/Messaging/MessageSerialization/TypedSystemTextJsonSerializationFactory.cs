#if NET8_0_OR_GREATER
using System.Text.Json;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

public class TypedSystemTextJsonSerializationFactory(JsonSerializerOptions options) : IMessageSerializationFactory
{
    public IMessageSerializer GetSerializer<T>() where T : Message => new SystemTextJsonSerializer<T>(options);
}
#endif
