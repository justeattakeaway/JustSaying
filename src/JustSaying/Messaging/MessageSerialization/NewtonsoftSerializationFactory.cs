using JustSaying.Models;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialization;

public class NewtonsoftSerializationFactory : IMessageSerializationFactory
{
    private readonly NewtonsoftSerializer _serializer;

    public NewtonsoftSerializationFactory()
        : this(null)
    {
    }

    public NewtonsoftSerializationFactory(JsonSerializerSettings settings)
    {
        _serializer = new NewtonsoftSerializer(settings);
    }

    public IMessageSerializer GetSerializer<TMessage>() where TMessage : class => _serializer;
}
