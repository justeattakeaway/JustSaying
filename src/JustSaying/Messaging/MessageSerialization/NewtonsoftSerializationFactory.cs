using JustSaying.Models;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialization;

#if NET8_0_OR_GREATER
[RequiresUnreferencedCode(Constants.SerializationUnreferencedCodeMessage)]
[RequiresDynamicCode(Constants.SerializationDynamicCodeMessage)]
#endif
public class NewtonsoftSerializationFactory(JsonSerializerSettings settings) : IMessageSerializationFactory
{
    private readonly NewtonsoftSerializer _serializer = new(settings);

    public NewtonsoftSerializationFactory()
        : this(null)
    {
    }

    public IMessageSerializer GetSerializer<T>() where T : Message => _serializer;
}
