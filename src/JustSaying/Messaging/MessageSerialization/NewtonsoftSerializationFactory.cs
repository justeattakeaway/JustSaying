using System.Collections.Concurrent;
using JustSaying.Models;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialization;

public sealed class NewtonsoftSerializationFactory(JsonSerializerSettings settings = null) : IMessageBodySerializationFactory
{
    private readonly ConcurrentDictionary<Type, object> _cache = new();

    public IMessageBodySerializer GetSerializer<T>() where T : Message => (IMessageBodySerializer)_cache.GetOrAdd(typeof(T), _ => new NewtonsoftMessageBodySerializer<T>(settings));
}
