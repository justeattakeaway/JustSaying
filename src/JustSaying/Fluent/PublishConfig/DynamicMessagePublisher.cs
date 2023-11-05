using System.Collections.Concurrent;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

// TODO review object vs generic type argument
internal sealed class DynamicMessagePublisher<TMessage> : IMessagePublisher<TMessage> where TMessage : class
{
    private readonly ConcurrentDictionary<string, IMessagePublisher<TMessage>> _publisherCache = new();
    private readonly Func<TMessage, string> _topicNameCustomizer;
    private readonly Func<string, StaticPublicationConfiguration<TMessage>> _staticConfigBuilder;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _topicCreationLocks = new();
    private readonly ILogger<DynamicMessagePublisher<TMessage>> _logger;

    public DynamicMessagePublisher(Func<TMessage, string> topicNameCustomizer, Func<string, StaticPublicationConfiguration<TMessage>> staticConfigBuilder, ILoggerFactory loggerFactory)
    {
        _topicNameCustomizer = topicNameCustomizer;
        _staticConfigBuilder = staticConfigBuilder;
        _logger = loggerFactory.CreateLogger<DynamicMessagePublisher<TMessage>>();
    }

    public InterrogationResult Interrogate()
    {
        var pairs = _publisherCache.Keys.OrderBy(x => x)
            .ToDictionary(x => x, x => _publisherCache[x].Interrogate());

        return new InterrogationResult(new
        {
            Publishers = pairs
        });
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public async Task PublishAsync(TMessage message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        var topicName = _topicNameCustomizer(message);
        if (_publisherCache.TryGetValue(topicName, out var publisher))
        {
            await publisher.PublishAsync(message, metadata, cancellationToken).ConfigureAwait(false);
            return;
        }

        var lockObj = _topicCreationLocks.GetOrAdd(topicName, _ => new SemaphoreSlim(1, 1));

        _logger.LogDebug("Publisher for topic {TopicName} not found, waiting on creation lock", topicName);
        await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (_publisherCache.TryGetValue(topicName, out var thePublisher))
        {
            _logger.LogDebug("Lock re-entrancy detected, returning existing publisher");
            await thePublisher.PublishAsync(message, metadata, cancellationToken).ConfigureAwait(false);
            return;
        }

        _logger.LogDebug("Lock acquired to initialize topic {TopicName}", topicName);
        var config = _staticConfigBuilder(topicName);
        _logger.LogDebug("Executing startup task for topic {TopicName}", topicName);
        await config.StartupTask(cancellationToken).ConfigureAwait(false);

        _ = _publisherCache.TryAdd(topicName, config.Publisher);

        _logger.LogDebug("Publishing message on newly created topic {TopicName}", topicName);
        await config.Publisher.PublishAsync(message, metadata, cancellationToken).ConfigureAwait(false);
    }

    public Task PublishAsync(TMessage message, CancellationToken cancellationToken)
        => PublishAsync(message, null, cancellationToken);
}
