using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka;

/// <summary>
/// Represents a Kafka topic as a message source for JustSaying subscription groups.
/// </summary>
public sealed class KafkaSource : IMessageSource
{
    public KafkaMessageConsumer Consumer { get; set; }
    public string Topic { get; set; }
    
    string IMessageSource.Name => Topic ?? string.Empty;

    public KafkaSource(
        string topic,
        KafkaConfiguration configuration,
        IMessageBodySerializationFactory serializationFactory,
        ILoggerFactory loggerFactory)
    {
        Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        Consumer = new KafkaMessageConsumer(topic, configuration, serializationFactory, loggerFactory);
    }
}
