using System.Collections.Concurrent;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

internal sealed class DynamicAddressMessagePublisher(
    string topicArnTemplate,
    Func<string, Message, string> topicAddressCustomizer,
    Func<string, StaticAddressPublicationConfiguration> staticConfigBuilder,
    ILoggerFactory loggerFactory) : IMessagePublisher, IMessageBatchPublisher
{
    private readonly string _topicArnTemplate = topicArnTemplate;
    private readonly ConcurrentDictionary<string, IMessagePublisher> _publisherCache = new();
    private readonly ConcurrentDictionary<string, IMessageBatchPublisher> _batchPublisherCache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _topicCreationLocks = new();
    private readonly ILogger<DynamicMessagePublisher> _logger = loggerFactory.CreateLogger<DynamicMessagePublisher>();
    private readonly Func<string, Message, string> _topicAddressCustomizer = topicAddressCustomizer;
    private readonly Func<string, StaticAddressPublicationConfiguration> _staticConfigBuilder = staticConfigBuilder;

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
        string topicArn = _topicAddressCustomizer(_topicArnTemplate, message);
        if (_publisherCache.TryGetValue(topicArn, out var publisher))
        {
            await publisher.PublishAsync(message, metadata, cancellationToken).ConfigureAwait(false);
            return;
        }

        var lockObj = _topicCreationLocks.GetOrAdd(topicArn, _ => new SemaphoreSlim(1, 1));

        _logger.LogDebug("Publisher for topic {TopicArn} not found, waiting on setup lock", topicArn);
        await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (_publisherCache.TryGetValue(topicArn, out var thePublisher))
        {
            _logger.LogDebug("Lock re-entrancy detected, returning existing publisher");
            await thePublisher.PublishAsync(message, metadata, cancellationToken).ConfigureAwait(false);
            return;
        }

        _logger.LogDebug("Lock acquired to configure topic {TopicArn}", topicArn);
        var config = _staticConfigBuilder(topicArn);

        _ = _publisherCache.TryAdd(topicArn, config.Publisher);
        lockObj.Release(1);

        _logger.LogDebug("Publishing message on newly configured topic {TopicArn}", topicArn);
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
            foreach (var groupByTopic in groupByType.GroupBy(x => _topicAddressCustomizer(_topicArnTemplate, x)))
            {
                string topicArn = groupByTopic.Key;
                var batch = groupByTopic.ToList();

                if (_batchPublisherCache.TryGetValue(topicArn, out var publisher))
                {
                    publisherTask.Add(publisher.PublishAsync(batch, metadata, cancellationToken));
                    continue;
                }

                var lockObj = _topicCreationLocks.GetOrAdd(topicArn, _ => new SemaphoreSlim(1, 1));
                _logger.LogDebug("Publisher for topic {TopicArn} not found, waiting on creation lock", topicArn);
                await lockObj.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (_batchPublisherCache.TryGetValue(topicArn, out publisher))
                {
                    _logger.LogDebug("Lock re-entrancy detected, returning existing publisher");
                    publisherTask.Add(publisher.PublishAsync(batch, metadata, cancellationToken));
                    continue;
                }

                _logger.LogDebug("Lock acquired to configure topic {TopicArn}", topicArn);
                var config = _staticConfigBuilder(topicArn);

                var cachedPublisher = _batchPublisherCache.GetOrAdd(topicArn, config.BatchPublisher);
                lockObj.Release(1);

                _logger.LogDebug("Publishing message on newly created topic {TopicName}", topicArn);
                publisherTask.Add(cachedPublisher.PublishAsync(batch, metadata, cancellationToken));
            }
        }

        await Task.WhenAll(publisherTask).ConfigureAwait(false);
    }
}
