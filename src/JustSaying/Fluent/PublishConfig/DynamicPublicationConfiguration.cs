using JustSaying.Messaging;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

internal sealed class DynamicPublicationConfiguration<T> : ITopicPublisher<T>  where T : class
{
    public DynamicPublicationConfiguration(IMessagePublisher<T> publisher)
    {
        Publisher = publisher;
    }

    public Func<CancellationToken, Task> StartupTask => _ => Task.CompletedTask;
    public IMessagePublisher<T> Publisher { get; }

    public static DynamicPublicationConfiguration<T> Build(
        Func<T, string> topicNameCustomizer,
        Func<string, StaticPublicationConfiguration<T>> staticConfigBuilder,
        ILoggerFactory loggerFactory)
    {
        var publisher = new DynamicMessagePublisher<T>(topicNameCustomizer, staticConfigBuilder, loggerFactory);

        return new DynamicPublicationConfiguration<T>(publisher);
    }
}
