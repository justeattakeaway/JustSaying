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
    private readonly ConcurrentDictionary<string, Lazy<StaticAddressPublicationConfiguration>> _publisherConfigurationCache = new();
    private readonly ILogger<DynamicMessagePublisher> _logger = loggerFactory.CreateLogger<DynamicMessagePublisher>();
    private readonly Func<string, Message, string> _topicAddressCustomizer = topicAddressCustomizer;
    private readonly Func<string, StaticAddressPublicationConfiguration> _staticConfigBuilder = staticConfigBuilder;

    /// <inheritdoc/>
    public InterrogationResult Interrogate()
    {
        var publishers = _publisherConfigurationCache.Keys.OrderBy(x => x).ToDictionary(x => x, x => _publisherConfigurationCache[x].Value.Publisher.Interrogate());

        return new InterrogationResult(new
        {
            Publishers = publishers,
        });
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        string topicArn = _topicAddressCustomizer(_topicArnTemplate, message);
        var publisherConfig = _publisherConfigurationCache.GetOrAdd(topicArn, CreateLazyPublisherConfig);
        await publisherConfig.Value.Publisher.PublishAsync(message, metadata, cancellationToken);
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

                var publisherConfig = _publisherConfigurationCache.GetOrAdd(topicArn, CreateLazyPublisherConfig);
                publisherTask.Add(publisherConfig.Value.BatchPublisher.PublishAsync(batch, metadata, cancellationToken));
            }
        }

        await Task.WhenAll(publisherTask).ConfigureAwait(false);
    }

    private Lazy<StaticAddressPublicationConfiguration> CreateLazyPublisherConfig(string topicArn)
        => new(() =>
        {
            _logger.LogDebug("Publisher configuration for topic {TopicArn} not found. Creating new configuration", topicArn);
            return _staticConfigBuilder(topicArn);
        });
}
