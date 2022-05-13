using System.Collections.Concurrent;
using System.ComponentModel;
using System.Xml.Linq;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

internal class DynamicMessagePublisher : IMessagePublisher
{
    private readonly Dictionary<string, IMessagePublisher> _publisherCache = new();
    private readonly Func<Message, string> _topicNameCustomizer;
    private readonly Func<string, StaticPublicationConfiguration> _staticConfigBuilder;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _topicCreationLocks = new();
    private readonly ILogger<DynamicMessagePublisher> _logger;

    public DynamicMessagePublisher(Func<Message, string> topicNameCustomizer, Func<string, StaticPublicationConfiguration> staticConfigBuilder, ILoggerFactory loggerFactory)
    {
        _topicNameCustomizer = topicNameCustomizer;
        _staticConfigBuilder = staticConfigBuilder;
        _logger = loggerFactory.CreateLogger<DynamicMessagePublisher>();
    }

    public InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {

        });
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        var topicName = _topicNameCustomizer(message);
        if (_publisherCache.ContainsKey(topicName))
        {
            await _publisherCache[topicName].PublishAsync(message, metadata, cancellationToken);
            return;
        }

        var lockObj = _topicCreationLocks.GetOrAdd(topicName, _ => new SemaphoreSlim(1, 1));


        _logger.LogDebug("Publisher for topic {TopicName} not found, waiting on creation lock", topicName);
        await lockObj.WaitAsync(cancellationToken);
        if (_publisherCache.ContainsKey(topicName))
        {
            _logger.LogDebug("Lock re-entrancy detected, returning existing publisher");
            await _publisherCache[topicName].PublishAsync(message, metadata, cancellationToken);
            return;
        }

        _logger.LogDebug("Lock acquired to init topic {TopicName}", topicName);
        var config = _staticConfigBuilder(topicName);
        _logger.LogDebug("Executing startup task for topic {TopicName}", topicName);
        await config.StartupTask(cancellationToken);

        _publisherCache.Add(topicName, config.Publisher);

        _logger.LogDebug("Publishing message on newly created topic {TopicName}", topicName);
        await _publisherCache[topicName].PublishAsync(message, metadata, cancellationToken);
    }

    public Task PublishAsync(Message message, CancellationToken cancellationToken)
        => PublishAsync(message, null, cancellationToken);
}

internal class DynamicPublicationConfiguration : TopicPublisher
{
    public DynamicPublicationConfiguration(IMessagePublisher publisher)
    {
        Publisher = publisher;
    }

    public Func<CancellationToken, Task> StartupTask => _ => Task.CompletedTask;
    public IMessagePublisher Publisher { get; }

    public static DynamicPublicationConfiguration Build<T>(
        Func<Message, string> topicNameCustomizer,
        Func<string, StaticPublicationConfiguration> staticConfigBuilder,
        ILoggerFactory loggerFactory)
    {
        var publisher = new DynamicMessagePublisher(topicNameCustomizer, staticConfigBuilder, loggerFactory);

        return new DynamicPublicationConfiguration(publisher);
    }
}
