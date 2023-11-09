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

public sealed class JustSayingBus : IMessagingBus, IMessagePublisher, IDisposable
{
    private readonly ILogger _log;
    private readonly ILoggerFactory _loggerFactory;

    private readonly SemaphoreSlim _startLock = new(1, 1);
    private bool _busStarted;
    private readonly List<Func<CancellationToken, Task>> _startupTasks;

    private ConcurrentDictionary<string, SubscriptionGroupConfigBuilder> _subscriptionGroupSettings;
    private SubscriptionGroupSettingsBuilder _defaultSubscriptionGroupSettings;
    private readonly Dictionary<Type, IMessagePublisher> _publishersByType;

    public IMessagingConfig Config { get; }

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
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

        _startupTasks = [];
        _log = _loggerFactory.CreateLogger("JustSaying");

        Config = config;
        SerializationRegister = serializationRegister;
        MiddlewareMap = new MiddlewareMap();

        _publishersByType = [];
        _subscriptionGroupSettings =
            new ConcurrentDictionary<string, SubscriptionGroupConfigBuilder>(StringComparer.Ordinal);
        _defaultSubscriptionGroupSettings = new SubscriptionGroupSettingsBuilder();
    }

    public JustSayingBus(
        IMessagingConfig config,
        IMessageSerializationRegister serializationRegister,
        IMessageReceivePauseSignal messageReceivePauseSignal,
        ILoggerFactory loggerFactory,
        IMessageMonitor monitor) : this(config, serializationRegister, loggerFactory, monitor)
    {
        _messageReceivePauseSignal = messageReceivePauseSignal;
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
    }

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
            _log.LogDebug("Suppressed an exception of type {ExceptionType} which likely " +
                          "means the bus is shutting down.", nameof(OperationCanceledException));
            // Don't bubble cancellation up to Completion task
        }
    }

    public async Task PublishAsync(Message message, CancellationToken cancellationToken)
        => await PublishAsync(message, null, cancellationToken).ConfigureAwait(false);

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

    public void Dispose()
    {
        _startLock?.Dispose();
        _loggerFactory?.Dispose();
    }
}
