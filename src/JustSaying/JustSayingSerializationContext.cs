#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying;

[JsonSerializable(typeof(SqsMessageEnvelope))]
[JsonSerializable(typeof(IReadOnlyCollection<string>))]
[JsonSerializable(typeof(RedrivePolicy))]
internal sealed partial class JustSayingSerializationContext : JsonSerializerContext;
#endif
