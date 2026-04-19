using System.Collections.Concurrent;
using JustSaying.Models;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialization;

public sealed class NewtonsoftSerializationFactory(JsonSerializerSettings settings = null) : IMessageBodySerializationFactory
{
    private readonly ConcurrentDictionary<Type, IMessageBodySerializer> _cache = new();

    public IMessageBodySerializer GetSerializer<T>() where T : Message => _cache.GetOrAdd(typeof(T), _ => new NewtonsoftMessageBodySerializer<T>(settings));
}
