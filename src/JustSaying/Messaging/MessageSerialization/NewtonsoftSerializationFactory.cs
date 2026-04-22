using System.Collections.Concurrent;
using JustSaying.Models;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialization;

public sealed class NewtonsoftSerializationFactory(JsonSerializerSettings settings = null) : IMessageBodySerializationFactory
{
    private readonly ConcurrentDictionary<Type, IMessageBodySerializer> _cache = new();

#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Caller has accepted Newtonsoft's trimming requirements by selecting this factory.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Caller has accepted Newtonsoft's dynamic code requirements by selecting this factory.")]
#endif
    public IMessageBodySerializer GetSerializer<T>() where T : Message => _cache.GetOrAdd(typeof(T), _ => new NewtonsoftMessageBodySerializer<T>(settings));
}
