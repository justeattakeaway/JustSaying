#if NET8_0_OR_GREATER
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

public class TypedSystemTextJsonSerializationFactory(JustSayingJsonSerializerOptions options) : IMessageSerializationFactory
{
    public IMessageSerializer GetSerializer<T>() where T : Message => new SystemTextJsonSerializer<T>(options.SerializerOptions);
}
#endif
