using System.Text.Json;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

#if NET8_0_OR_GREATER
[RequiresUnreferencedCode(Constants.SerializationUnreferencedCodeMessage)]
[RequiresDynamicCode(Constants.SerializationDynamicCodeMessage)]
#endif
public class SystemTextJsonSerializationFactory(JsonSerializerOptions options) : IMessageSerializationFactory
{
    private readonly SystemTextJsonSerializer _serializer = new(options);

    public SystemTextJsonSerializationFactory()
        : this(null)
    {
    }

    public IMessageSerializer GetSerializer<T>() where T : Message => _serializer;
}
