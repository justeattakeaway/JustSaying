using System.Collections.Concurrent;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Extensions;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.HandleMessageContext, bool>;

namespace JustSaying;

public sealed class JustSayingBus : IMessagingBus, IMessagePublisher, IMessageBatchPublisher, IDisposable
{
    private readonly ILogger _log;
    private readonly ILoggerFactory _loggerFactory;

    private readonly SemaphoreSlim _startLock = new(1, 1);
    private bool _busStarted;
    private readonly List<Func<CancellationToken, Task>> _startupTasks;

    private ConcurrentDictionary<string, SubscriptionGroupConfigBuilder> _subscriptionGroupSettings;
    private SubscriptionGroupSettingsBuilder _defaultSubscriptionGroupSettings;
    private readonly Dictionary<Type, IMessagePublisher> _publishersByType;
    private readonly Dictionary<Type, IMessageBatchPublisher> _batchPublishersByType;

    public IMessagingConfig Config { get; }
    public IPublishBatchConfiguration PublishBatchConfiguration { get; }

    private readonly IMessageReceivePauseSignal _messageReceivePauseSignal;

    private readonly IMessageMonitor _monitor;

    private ISubscriptionGroup SubscriptionGroups { get; set; }
    public IMessageSerializationRegister SerializationRegister { get; }

    internal MiddlewareMap MiddlewareMap { get; }

    public Task Completion { get; private set; }

    public JustSayingBus(
        IMessagingConfig config,
        IMessageSerializationRegister serializationRegister,
        ILoggerFactory loggerFactory,
        IMessageMonitor monitor)
        : this(config, serializationRegister, null, loggerFactory, monitor, config as IPublishBatchConfiguration)
    {
    }

    public JustSayingBus(
        IMessagingConfig config,
        IMessageSerializationRegister serializationRegister,
        IMessageReceivePauseSignal messageReceivePauseSignal,
        ILoggerFactory loggerFactory,
        IMessageMonitor monitor) : this(config, serializationRegister, messageReceivePauseSignal, loggerFactory, monitor, config as IPublishBatchConfiguration)
    {
    }

    public JustSayingBus(
        IMessagingConfig config,
        IMessageSerializationRegister serializationRegister,
        IMessageReceivePauseSignal messageReceivePauseSignal,
        ILoggerFactory loggerFactory,
        IMessageMonitor monitor,
        IPublishBatchConfiguration publishBatchConfiguration)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

        _startupTasks = [];
        _log = _loggerFactory.CreateLogger("JustSaying");
        _messageReceivePauseSignal = messageReceivePauseSignal;

        Config = config;
        PublishBatchConfiguration = publishBatchConfiguration;
        if (PublishBatchConfiguration == null)
        {
            if (config is IPublishBatchConfiguration batchConfig)
            {
                PublishBatchConfiguration = batchConfig;
            }
            else
            {
                PublishBatchConfiguration = new MessagingConfig();
            }
        }

        SerializationRegister = serializationRegister;
        MiddlewareMap = new MiddlewareMap();

        _publishersByType = [];
        _batchPublishersByType = [];
        _subscriptionGroupSettings = new ConcurrentDictionary<string, SubscriptionGroupConfigBuilder>(StringComparer.Ordinal);
        _defaultSubscriptionGroupSettings = new SubscriptionGroupSettingsBuilder();
    }

    public void AddQueue(string subscriptionGroup, ISqsQueue queue)
    {
        if (string.IsNullOrWhiteSpace(subscriptionGroup))
        {
            throw new ArgumentException("Cannot be null or empty.", nameof(subscriptionGroup));
        }

        if (queue == null)
        {
            throw new ArgumentNullException(nameof(queue));
        }

        SubscriptionGroupConfigBuilder builder = _subscriptionGroupSettings.GetOrAdd(
            subscriptionGroup,
            _ => new SubscriptionGroupConfigBuilder(subscriptionGroup));

        builder.AddQueue(queue);
    }

    internal void AddStartupTask(Func<CancellationToken, Task> task)
    {
        _startupTasks.Add(task);
    }

    public void SetGroupSettings(
        SubscriptionGroupSettingsBuilder defaults,
        IDictionary<string, SubscriptionGroupConfigBuilder> settings)
    {
        _defaultSubscriptionGroupSettings = defaults;
        _subscriptionGroupSettings =
            new ConcurrentDictionary<string, SubscriptionGroupConfigBuilder>(settings);
    }

    public void AddMessageMiddleware<T>(string queueName, HandleMessageMiddleware middleware)
        where T : Message
    {
        SerializationRegister.AddSerializer<T>();
        MiddlewareMap.Add<T>(queueName, middleware);
    }

    public void AddMessagePublisher<T>(IMessagePublisher messagePublisher) where T : Message
    {
        if (Config.PublishFailureReAttempts == 0)
        {
            _log.LogWarning(
                "You have not set a re-attempt value for publish failures. If the publish location is 'down' you may lose messages.");
        }

        _publishersByType[typeof(T)] = messagePublisher;
        if (messagePublisher is IMessageBatchPublisher batchPublisher)
        {
            _batchPublishersByType[typeof(T)] = batchPublisher;
        }
    }

    public void AddMessageBatchPublisher<T>(IMessageBatchPublisher messageBatchPublisher) where T : Message
    {
        if (PublishBatchConfiguration.PublishFailureReAttempts == 0)
        {
            _log.LogWarning("You have not set a re-attempt value for publish failures. If the publish location is not available you may lose messages.");
        }

        _batchPublishersByType[typeof(T)] = messageBatchPublisher;
        if (messageBatchPublisher is IMessagePublisher messagePublisher)
        {
            _publishersByType[typeof(T)] = messagePublisher;
        }
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested) return;

        // Double check lock to ensure single-start
        if (!_busStarted)
        {
            await _startLock.WaitAsync(stoppingToken).ConfigureAwait(false);
            try
            {
                if (!_busStarted)
                {
                    using (_log.Time("Starting bus"))
                    {
                        // We want consumers to wait for the startup tasks, but not the run
                        using (_log.Time("Running {TaskCount} startup tasks", _startupTasks.Count))
                        {
                            foreach (var startupTask in _startupTasks)
                            {
                                await startupTask.Invoke(stoppingToken).ConfigureAwait(false);
                            }
                        }

                        Completion = RunImplAsync(stoppingToken);
                        _busStarted = true;
                    }
                }
            }
            finally
            {
                _startLock.Release();
            }
        }
    }

    private async Task RunImplAsync(CancellationToken stoppingToken)
    {
        var dispatcher = new MessageDispatcher(
            SerializationRegister,
            _monitor,
            MiddlewareMap,
            _loggerFactory);

        var subscriptionGroupFactory = new SubscriptionGroupFactory(
            dispatcher,
            _messageReceivePauseSignal,
            _monitor,
            _loggerFactory);

        SubscriptionGroups =
            subscriptionGroupFactory.Create(_defaultSubscriptionGroupSettings,
                _subscriptionGroupSettings);

        _log.LogInformation("Starting bus with settings: {@Response}", SubscriptionGroups.Interrogate());

        try
        {
            await SubscriptionGroups.RunAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _log.LogDebug(
                "Suppressed an exception of type {ExceptionType} which likely means the bus is shutting down.",
                nameof(OperationCanceledException));
            // Don't bubble cancellation up to Completion task
        }
    }

    /// <inheritdoc/>
    public async Task PublishAsync(Message message, CancellationToken cancellationToken)
        => await PublishAsync(message, null, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task PublishAsync(
        Message message,
        PublishMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (!_busStarted && _startupTasks.Count > 0)
        {
            throw new InvalidOperationException("There are pending startup tasks that must be executed by calling StartAsync before messages may be published.");
        }

        IMessagePublisher publisher = GetPublisherForMessage(message);
        await PublishAsync(publisher, message, metadata, 0, cancellationToken)
            .ConfigureAwait(false);
    }

    private IMessagePublisher GetPublisherForMessage(Message message)
    {
        if (_publishersByType.Count == 0)
        {
            _log.LogError("Error publishing message, no publishers registered. Has the bus been started?");
            throw new InvalidOperationException("Error publishing message, no publishers registered. Has the bus been started?");
        }

        var messageType = message.GetType();

        var publishersFound =
            _publishersByType.TryGetValue(messageType, out var publisher);
        if (!publishersFound)
        {
            _log.LogError(
                "Error publishing message. No publishers registered for message type '{MessageType}'.",
                messageType);

            throw new InvalidOperationException(
                $"Error publishing message, no publishers registered for message type '{messageType}'.");
        }

        return publisher;
    }

    private async Task PublishAsync(
        IMessagePublisher publisher,
        Message message,
        PublishMetadata metadata,
        int attemptCount,
        CancellationToken cancellationToken)
    {
        attemptCount++;
        try
        {
            using (_monitor.MeasurePublish())
            {
                await publisher.PublishAsync(message, metadata, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            var messageType = message.GetType();

            if (attemptCount >= Config.PublishFailureReAttempts)
            {
                _monitor.IssuePublishingMessage();

                _log.LogError(
                    ex,
                    "Failed to publish a message of type '{MessageType}'. Halting after attempt number {PublishAttemptCount}.",
                    messageType,
                    attemptCount);

                throw;
            }

            _log.LogWarning(
                ex,
                "Failed to publish a message of type '{MessageType}'. Retrying after attempt number {PublishAttemptCount} of {PublishFailureReattempts}.",
                messageType,
                attemptCount,
                Config.PublishFailureReAttempts);

            var delayForAttempt =
                TimeSpan.FromMilliseconds(Config.PublishFailureBackoff.TotalMilliseconds * attemptCount);
            await Task.Delay(delayForAttempt, cancellationToken).ConfigureAwait(false);

            await PublishAsync(publisher, message, metadata, attemptCount, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public InterrogationResult Interrogate()
    {
        var publisherDescriptions =
            _publishersByType.ToDictionary(x => x.Key.Name, x => x.Value.Interrogate());

        return new InterrogationResult(new
        {
            Config.Region,
            Middleware = MiddlewareMap.Interrogate(),
            PublishedMessageTypes = publisherDescriptions,
            SubscriptionGroups = SubscriptionGroups?.Interrogate()
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _startLock?.Dispose();
        _loggerFactory?.Dispose();
    }

    /// <inheritdoc/>
    public Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
    {
        if (!_busStarted && _startupTasks.Count > 0)
        {
            throw new InvalidOperationException("There are pending startup tasks that must be executed by calling StartAsync before messages may be published.");
        }

        var tasks = new List<Task>();
        foreach (IGrouping<Type, Message> group in messages.GroupBy(x => x.GetType()))
        {
            IMessageBatchPublisher publisher = GetBatchPublishersForMessageType(group.Key);
            tasks.Add(PublishAsync(publisher, group.ToList(), metadata, 0, group.Key, cancellationToken));
        }

        return Task.WhenAll(tasks);
    }

    private IMessageBatchPublisher GetBatchPublishersForMessageType(Type messageType)
    {
        if (_publishersByType.Count == 0)
        {
            const string errorMessage = "Error publishing message, no publishers registered. Has the bus been started?";
            _log.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        if (!_batchPublishersByType.TryGetValue(messageType, out var publisher))
        {
            _log.LogError("Error publishing message. No publishers registered for message type '{MessageType}'.", messageType);
            throw new InvalidOperationException($"Error publishing message, no publishers registered for message type '{messageType}'.");
        }

        return publisher;
    }

    private async Task PublishAsync(
        IMessageBatchPublisher publisher,
        List<Message> messages,
        PublishBatchMetadata metadata,
        int attemptCount,
        Type messageType,
        CancellationToken cancellationToken)
    {
        var batchSize = metadata?.BatchSize ?? 10;
        batchSize = Math.Min(batchSize, 10);
        attemptCount++;

        foreach (var chunk in messages.Chunk(batchSize))
        {
            try
            {
                using (_monitor.MeasurePublish())
                {
                    await publisher.PublishAsync(chunk, metadata, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (attemptCount >= PublishBatchConfiguration.PublishFailureReAttempts)
                {
                    _monitor.IssuePublishingMessage();

                    _log.LogError(
                        ex,
                        "Failed to publish a message of type '{MessageType}'. Halting after attempt number {PublishAttemptCount}.",
                        messageType,
                        attemptCount);

                    throw;
                }

                _log.LogWarning(
                    ex,
                    "Failed to publish a message of type '{MessageType}'. Retrying after attempt number {PublishAttemptCount} of {PublishFailureReattempts}.",
                    messageType,
                    attemptCount,
                    PublishBatchConfiguration.PublishFailureReAttempts);

                var delayForAttempt = TimeSpan.FromMilliseconds(Config.PublishFailureBackoff.TotalMilliseconds * attemptCount);
                await Task.Delay(delayForAttempt, cancellationToken).ConfigureAwait(false);

                await PublishAsync(publisher, messages, metadata, attemptCount, messageType, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
