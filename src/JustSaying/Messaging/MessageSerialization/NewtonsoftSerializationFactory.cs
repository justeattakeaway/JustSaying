using JustSaying.Models;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialization;

public class NewtonsoftSerializationFactory(JsonSerializerSettings settings) : IMessageSerializationFactory
{
    private readonly NewtonsoftSerializer _serializer = new(settings);

    public NewtonsoftSerializationFactory()
        : this(null)
    {
    }

    public IMessageSerializer GetSerializer<T>() where T : Message => _serializer;
}
