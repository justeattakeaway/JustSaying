using System.Collections.Concurrent;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

internal sealed class DynamicMessagePublisher(
    Func<Message, string> topicNameCustomizer,
    Func<string, StaticPublicationConfiguration> staticConfigBuilder,
    ILoggerFactory loggerFactory) : IMessagePublisher, IMessageBatchPublisher
{
    private readonly ConcurrentDictionary<string, IMessagePublisher> _publisherCache = new();
    private readonly ConcurrentDictionary<string, IMessageBatchPublisher> _batchPublisherCache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _topicCreationLocks = new();
    private readonly ILogger<DynamicMessagePublisher> _logger = loggerFactory.CreateLogger<DynamicMessagePublisher>();
    private readonly Func<Message, string> _topicNameCustomizer = topicNameCustomizer;
    private readonly Func<string, StaticPublicationConfiguration> _staticConfigBuilder = staticConfigBuilder;

    /// <inheritdoc/>
    public InterrogationResult Interrogate()
    {
        var publishers = _publisherCache.Keys.OrderBy(x => x).ToDictionary(x => x, x => _publisherCache[x].Interrogate());
        var batchPublishers = _batchPublisherCache.Keys.OrderBy(x => x).ToDictionary(x => x, x => _batchPublisherCache[x].Interrogate());

        return new InterrogationResult(new
        {
            Publishers = publishers,
            BatchPublishers = batchPublishers,
        });
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        string topicName = _topicNameCustomizer(message);
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

    /// <inheritdoc/>
    public Task PublishAsync(Message message, CancellationToken cancellationToken)
        => PublishAsync(message, null, cancellationToken);

    /// <inheritdoc/>
    public async Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
    {
        var publisherTask = new List<Task>();
        foreach (var groupByType in messages.GroupBy(x => x.GetType()))
        {
            foreach (var groupByTopic in groupByType.GroupBy(x => _topicNameCustomizer(x)))
            {
                string topicName = groupByTopic.Key;
                var batch = groupByTopic.ToList();

                if (_batchPublisherCache.TryGetValue(topicName, out var publisher))
                {
                    publisherTask.Add(publisher.PublishAsync(batch, metadata, cancellationToken));
                    continue;
                }

                var lockObj = _topicCreationLocks.GetOrAdd(topicName, _ => new SemaphoreSlim(1, 1));
                _logger.LogDebug("Publisher for topic {TopicName} not found, waiting on creation lock", topicName);
                await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (_batchPublisherCache.TryGetValue(topicName, out publisher))
                {
                    _logger.LogDebug("Lock re-entrancy detected, returning existing publisher");
                    publisherTask.Add(publisher.PublishAsync(batch, metadata, cancellationToken));
                    continue;
                }

                _logger.LogDebug("Lock acquired to initialize topic {TopicName}", topicName);
                var config = _staticConfigBuilder(topicName);
                _logger.LogDebug("Executing startup task for topic {TopicName}", topicName);
                await config.StartupTask(cancellationToken).ConfigureAwait(false);

                var cachedPublisher = _batchPublisherCache.GetOrAdd(topicName, config.BatchPublisher);

                _logger.LogDebug("Publishing message on newly created topic {TopicName}", topicName);
                publisherTask.Add(cachedPublisher.PublishAsync(batch, metadata, cancellationToken));
            }
        }

        await Task.WhenAll(publisherTask).ConfigureAwait(false);
    }
}
