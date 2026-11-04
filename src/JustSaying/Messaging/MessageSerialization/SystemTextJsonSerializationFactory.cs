using System.Collections.Concurrent;
using System.Text.Json;

namespace JustSaying.Messaging.MessageSerialization;

public sealed class SystemTextJsonSerializationFactory(JsonSerializerOptions options) : IMessageBodySerializationFactory
{
    private readonly ConcurrentDictionary<Type, object> _cache = new();

    public IMessageBodySerializer<T> GetSerializer<T>() where T : class
        => (IMessageBodySerializer<T>)_cache.GetOrAdd(typeof(T), _ => new SystemTextJsonMessageBodySerializer<T>(options));
}
