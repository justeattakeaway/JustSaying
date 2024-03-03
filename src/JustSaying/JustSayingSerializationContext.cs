#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying;

[JsonSerializable(typeof(SqsMessageEnvelope))]
[JsonSerializable(typeof(IReadOnlyCollection<string>))]
#pragma warning disable CS0618 // Type or member is obsolete
[JsonSerializable(typeof(RedrivePolicy))]
#pragma warning restore CS0618 // Type or member is obsolete
internal sealed partial class JustSayingSerializationContext : JsonSerializerContext;
#endif
