using JustSaying.Messaging;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

internal sealed class DynamicPublicationConfiguration : ITopicPublisher
{
    public DynamicPublicationConfiguration(IMessagePublisher publisher, IMessageBatchPublisher batchPublisher)
    {
        Publisher = publisher;
        BatchPublisher = batchPublisher;
    }

    public Func<CancellationToken, Task> StartupTask => _ => Task.CompletedTask;
    public IMessagePublisher Publisher { get; }
    public IMessageBatchPublisher BatchPublisher { get; }

    public static DynamicPublicationConfiguration Build<T>(
        Func<Message, string> topicNameCustomizer,
        Func<string, StaticPublicationConfiguration> staticConfigBuilder,
        ILoggerFactory loggerFactory)
    {
        var publisher = new DynamicMessagePublisher(topicNameCustomizer, staticConfigBuilder, loggerFactory);

        return new DynamicPublicationConfiguration(publisher, publisher);
    }
}
