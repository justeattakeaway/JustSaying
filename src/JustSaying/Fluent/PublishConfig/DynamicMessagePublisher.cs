using System.Collections.Concurrent;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

internal sealed class DynamicMessagePublisher(
    Func<Message, string> topicNameCustomizer,
    Func<string, StaticPublicationConfiguration> staticConfigBuilder,
    ILoggerFactory loggerFactory) : IMessagePublisher
{
    private readonly ConcurrentDictionary<string, IMessagePublisher> _publisherCache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _topicCreationLocks = new();
    private readonly ILogger<DynamicMessagePublisher> _logger = loggerFactory.CreateLogger<DynamicMessagePublisher>();

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

    public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        var topicName = topicNameCustomizer(message);
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
        var config = staticConfigBuilder(topicName);
        _logger.LogDebug("Executing startup task for topic {TopicName}", topicName);
        await config.StartupTask(cancellationToken).ConfigureAwait(false);

        _ = _publisherCache.TryAdd(topicName, config.Publisher);

        _logger.LogDebug("Publishing message on newly created topic {TopicName}", topicName);
        await config.Publisher.PublishAsync(message, metadata, cancellationToken).ConfigureAwait(false);
    }

    public Task PublishAsync(Message message, CancellationToken cancellationToken)
        => PublishAsync(message, null, cancellationToken);
}
