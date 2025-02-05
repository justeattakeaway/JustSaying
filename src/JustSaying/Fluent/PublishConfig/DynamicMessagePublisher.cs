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
    private readonly ConcurrentDictionary<string, Lazy<Task<StaticPublicationConfiguration>>> _publisherConfigurationCache = new();
    private readonly ILogger<DynamicMessagePublisher> _logger = loggerFactory.CreateLogger<DynamicMessagePublisher>();
    private readonly Func<Message, string> _topicNameCustomizer = topicNameCustomizer;
    private readonly Func<string, StaticPublicationConfiguration> _staticConfigBuilder = staticConfigBuilder;

    /// <inheritdoc/>
    public InterrogationResult Interrogate()
    {
        var publishers = GetInterrogationResultForTasks(static config => config.Publisher.Interrogate());

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
        string topicName = _topicNameCustomizer(message);
        var publisherConfigTask = _publisherConfigurationCache.GetOrAdd(topicName, _ => CreateLazyPublisherConfig(topicName, cancellationToken)).Value;
        var publisherConfig = await publisherConfigTask;
        await publisherConfig.Publisher.PublishAsync(message, metadata, cancellationToken);
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
                var publisherConfigTask = _publisherConfigurationCache.GetOrAdd(topicName, _ => CreateLazyPublisherConfig(topicName, cancellationToken)).Value;
                var publisherConfig = await publisherConfigTask;
                publisherTask.Add(publisherConfig.BatchPublisher.PublishAsync(batch, metadata, cancellationToken));
            }
        }

        await Task.WhenAll(publisherTask).ConfigureAwait(false);
    }

    private Dictionary<string, InterrogationResult> GetInterrogationResultForTasks(Func<StaticPublicationConfiguration, InterrogationResult> interrogate) =>
        _publisherConfigurationCache
            .Where(w => w.Value.Value.IsCompleted)
            .OrderBy(o => o.Key)
            .ToDictionary(x => x.Key, x => interrogate(x.Value.Value.Result));

    private Lazy<Task<StaticPublicationConfiguration>> CreateLazyPublisherConfig(string topicName, CancellationToken cancellationToken)
        => new(async () =>
        {
            _logger.LogDebug("Publisher configuration for topic {TopicName} not found. Initializing Topic.", topicName);
            var config = _staticConfigBuilder(topicName);
            await config.StartupTask(cancellationToken).ConfigureAwait(false);
            return config;
        });
}
