using System.Text.Json;
using JustSaying.Models;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace JustSaying.Messaging.MessageSerialization;

public class SystemTextJsonSerializationFactory(JsonSerializerOptions options) : IMessageSerializationFactory
{
#pragma warning disable CS0618 // Keep using obsolete serializer for now until the next breaking version
#pragma warning disable IL2026 // Not used in contexts where dynamic code is not supported
#pragma warning disable IL3050
    private readonly SystemTextJsonSerializer _serializer = new(options);
#pragma warning restore IL3050
#pragma warning restore IL2026
#pragma warning restore CS0618

    public SystemTextJsonSerializationFactory()
        : this(null)
    {
    }

    public IMessageSerializer GetSerializer<T>() where T : Message =>
#if !NET8_0_OR_GREATER
        _serializer;
#else
        RuntimeFeature.IsDynamicCodeSupported
            ? _serializer
            : new SystemTextJsonSerializer<T>(options);
#endif
}
