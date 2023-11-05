using JustSaying.Messaging;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

internal sealed class DynamicPublicationConfiguration<TMessage> : ITopicPublisher<TMessage>  where TMessage : class
{
    public DynamicPublicationConfiguration(IMessagePublisher<TMessage> publisher)
    {
        Publisher = publisher;
    }

    public Func<CancellationToken, Task> StartupTask => _ => Task.CompletedTask;
    public IMessagePublisher<TMessage> Publisher { get; }

    public static DynamicPublicationConfiguration<TMessage> Build(
        Func<TMessage, string> topicNameCustomizer,
        Func<string, StaticPublicationConfiguration<TMessage>> staticConfigBuilder,
        ILoggerFactory loggerFactory)
    {
        var publisher = new DynamicMessagePublisher<TMessage>(topicNameCustomizer, staticConfigBuilder, loggerFactory);

        return new DynamicPublicationConfiguration<TMessage>(publisher);
    }
}
