using System.Collections.Concurrent;
using Newtonsoft.Json;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace JustSaying.Messaging.MessageSerialization;

public sealed class NewtonsoftSerializationFactory : IMessageBodySerializationFactory
{
#if NET8_0_OR_GREATER
    private const string NewtonsoftRequiresUnreferencedCodeMessage = "Newtonsoft.Json relies on reflection over types that may be removed when trimming.";
    private const string NewtonsoftRequiresDynamicCodeMessage = "Newtonsoft.Json relies on dynamically creating types that may not be available with Native AOT.";
#endif

    private readonly ConcurrentDictionary<Type, object> _cache = new();
    private readonly JsonSerializerSettings _settings;

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode(NewtonsoftRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(NewtonsoftRequiresDynamicCodeMessage)]
#endif
    public NewtonsoftSerializationFactory(JsonSerializerSettings settings = null)
    {
        _settings = settings;
    }

#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Caller already accepted Newtonsoft's trimming requirements when instantiating this factory.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Caller already accepted Newtonsoft's dynamic code requirements when instantiating this factory.")]
#endif
    public IMessageBodySerializer<T> GetSerializer<T>() where T : class
        => (IMessageBodySerializer<T>)_cache.GetOrAdd(typeof(T), _ => new NewtonsoftMessageBodySerializer<T>(_settings));
}
