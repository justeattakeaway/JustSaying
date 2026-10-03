using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialization;

public sealed class NewtonsoftSerializationFactory(JsonSerializerSettings settings = null) : IMessageBodySerializationFactory
{
    private readonly ConcurrentDictionary<Type, object> _cache = new();

    public IMessageBodySerializer<T> GetSerializer<T>() where T : class
        => (IMessageBodySerializer<T>)_cache.GetOrAdd(typeof(T), _ => new NewtonsoftMessageBodySerializer<T>(settings));
}
