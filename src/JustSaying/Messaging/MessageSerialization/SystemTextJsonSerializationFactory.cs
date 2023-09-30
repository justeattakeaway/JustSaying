using System.Text.Json;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

public class SystemTextJsonSerializationFactory(JsonSerializerOptions options) : IMessageSerializationFactory
{
    private readonly SystemTextJsonSerializer _serializer = new(options);

    public SystemTextJsonSerializationFactory()
        : this(null)
    {
    }

    public IMessageSerializer GetSerializer<T>() where T : Message => _serializer;
}
