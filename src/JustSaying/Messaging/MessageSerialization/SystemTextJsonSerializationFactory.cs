using System.Collections.Concurrent;
using System.Text.Json;

namespace JustSaying.Messaging.MessageSerialization;

public sealed class SystemTextJsonSerializationFactory(JsonSerializerOptions options) : IMessageBodySerializationFactory
{
    private readonly ConcurrentDictionary<Type, object> _cache = new();

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used by the serializers this factory creates,
    /// which describe the wire contract of the messages, for example for schema generation.
    /// </summary>
    public JsonSerializerOptions SerializerOptions => options;

    public IMessageBodySerializer<T> GetSerializer<T>() where T : class
        => (IMessageBodySerializer<T>)_cache.GetOrAdd(typeof(T), _ => new SystemTextJsonMessageBodySerializer<T>(options));
}
