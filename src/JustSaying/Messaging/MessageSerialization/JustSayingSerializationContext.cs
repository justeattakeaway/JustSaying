#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;

namespace JustSaying.Messaging.MessageSerialization;

[JsonSerializable(typeof(SqsMessageEnvelope))]
[JsonSerializable(typeof(IReadOnlyCollection<string>))]
internal sealed partial class JustSayingSerializationContext : JsonSerializerContext;
#endif
