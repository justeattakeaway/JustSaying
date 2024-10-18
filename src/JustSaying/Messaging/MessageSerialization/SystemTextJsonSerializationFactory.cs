using System.Collections.Concurrent;
using System.Text.Json;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

public sealed class SystemTextJsonSerializationFactory(JsonSerializerOptions options) : IMessageBodySerializationFactory
{
    private readonly ConcurrentDictionary<Type, object> _cache = new();

    public IMessageBodySerializer GetSerializer<T>() where T : Message => (IMessageBodySerializer)_cache.GetOrAdd(typeof(T), _ => new SystemTextJsonMessageBodySerializer<T>(options));
}
