using JustSaying.Messaging;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

internal sealed class DynamicAddressPublicationConfiguration(
    IMessagePublisher publisher,
    IMessageBatchPublisher batchPublisher) : ITopicAddressPublisher
{
    public IMessagePublisher Publisher { get; } = publisher;
    public IMessageBatchPublisher BatchPublisher { get; } = batchPublisher;

    public static DynamicAddressPublicationConfiguration Build<T>(
        string topicArnTemplate,
        Func<string, Message, string> topicNameCustomizer,
        Func<string, StaticAddressPublicationConfiguration> staticConfigBuilder,
        ILoggerFactory loggerFactory)
    {
        var publisher = new DynamicAddressMessagePublisher(topicArnTemplate, topicNameCustomizer, staticConfigBuilder, loggerFactory);

        return new DynamicAddressPublicationConfiguration(publisher, publisher);
    }
}
