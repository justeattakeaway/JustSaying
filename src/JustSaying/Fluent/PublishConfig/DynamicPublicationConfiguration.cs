using JustSaying.Messaging;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

internal sealed class DynamicPublicationConfiguration(IMessagePublisher publisher, IMessageBatchPublisher batchPublisher) : ITopicPublisher
{
    public Func<CancellationToken, Task> StartupTask => _ => Task.CompletedTask;
    public IMessagePublisher Publisher { get; } = publisher;
    public IMessageBatchPublisher BatchPublisher { get; } = batchPublisher;

    public static DynamicPublicationConfiguration Build<T>(
        Func<Message, string> topicNameCustomizer,
        Func<string, StaticPublicationConfiguration> staticConfigBuilder,
        ILoggerFactory loggerFactory)
    {
        var publisher = new DynamicMessagePublisher(topicNameCustomizer, staticConfigBuilder, loggerFactory);

        return new DynamicPublicationConfiguration(publisher, publisher);
    }
}
