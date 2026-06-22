using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Extensions;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.HandleMessageContext, bool>;
using PublishMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.PublishContext, bool>;

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
    private readonly Dictionary<Type, PublishMessageMiddleware> _publishMiddlewareByType;

    public IMessagingConfig Config { get; }
    public IPublishBatchConfiguration PublishBatchConfiguration { get; }

    private readonly IMessageReceivePauseSignal _messageReceivePauseSignal;

    private readonly IMessageMonitor _monitor;

    private ISubscriptionGroup SubscriptionGroups { get; set; }

    internal MiddlewareMap MiddlewareMap { get; }
    internal PublishMessageMiddleware PublishMiddleware { get; set; }
    internal MessageCompressionRegistry CompressionRegistry { get; }
    internal IMessageBodySerializationFactory MessageBodySerializerFactory { get; set; }

    /// <summary>
    /// Gets the provider that reads message identity for every publish and handle log, publish
    /// activity and batch publish of this bus: the configured one when the config is a
    /// <see cref="MessagingConfig"/>, otherwise the default.
    /// </summary>
    internal IMessageMetadataProvider MessageMetadataProvider
        => (Config as MessagingConfig)?.MessageMetadataProvider ?? DefaultMessageMetadataProvider.Instance;

    private IMessageTypeRegistry _messageTypeRegistry;

    /// <summary>
    /// Maps message types to their logical wire name (the SNS subject). Created lazily from the
    /// finalised <see cref="IMessagingConfig.MessageSubjectProvider"/>.
    /// </summary>
    internal IMessageTypeRegistry MessageTypeRegistry => _messageTypeRegistry ??= new MessageTypeRegistry(Config.MessageSubjectProvider);

    public Task Completion { get; private set; }

    internal JustSayingBus(
        IMessagingConfig config,
        IMessageBodySerializationFactory serializationFactory,
        ILoggerFactory loggerFactory,
        IMessageMonitor monitor)
        : this(config, serializationFactory, null, loggerFactory, monitor, config as IPublishBatchConfiguration)
    {
    }

    internal JustSayingBus(
        IMessagingConfig config,
        IMessageBodySerializationFactory serializationFactory,
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

        MiddlewareMap = new MiddlewareMap();
        CompressionRegistry = new MessageCompressionRegistry([new GzipMessageBodyCompression()]);
        MessageBodySerializerFactory = serializationFactory;

        _publishersByType = [];
        _batchPublishersByType = [];
        _publishMiddlewareByType = [];
        _subscriptionGroupSettings = new ConcurrentDictionary<string, SubscriptionGroupConfigBuilder>(StringComparer.Ordinal);
        _defaultSubscriptionGroupSettings = new SubscriptionGroupSettingsBuilder();
    }

    internal JustSayingBus(
        IMessagingConfig config,
        IMessageBodySerializationFactory serializationFactory,
        IMessageReceivePauseSignal messageReceivePauseSignal,
        ILoggerFactory loggerFactory,
        IMessageMonitor monitor) : this(config, serializationFactory, loggerFactory, monitor)
    {
        _messageReceivePauseSignal = messageReceivePauseSignal;
    }

    internal void AddQueue(string subscriptionGroup, SqsSource queue)
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

    public void AddMessageMiddleware<T>(string queueName, HandleMessageMiddleware middleware) where T : class
    {
        MiddlewareMap.Add<T>(queueName, middleware);
    }

    public void AddMessagePublisher<T>(IMessagePublisher messagePublisher) where T : class
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

    public void AddMessageBatchPublisher<T>(IMessageBatchPublisher messageBatchPublisher) where T : class
    {
        if (PublishBatchConfiguration.PublishFailureReAttempts == 0)
        {
            _log.LogWarning("You have not set a re-attempt value for batch publish failures. If the publish location is not available you may lose messages.");
        }

        _batchPublishersByType[typeof(T)] = messageBatchPublisher;
        if (messageBatchPublisher is IMessagePublisher messagePublisher)
        {
            _publishersByType[typeof(T)] = messagePublisher;
        }
    }

    internal void AddPublishMiddleware<T>(PublishMessageMiddleware middleware) where T : class
    {
        _publishMiddlewareByType[typeof(T)] = middleware;
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
    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : class
        => await PublishAsync(message, null, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task PublishAsync<TMessage>(
        TMessage message,
        PublishMetadata metadata,
        CancellationToken cancellationToken) where TMessage : class
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        EnsureStarted();

        var publicationType = GetPublicationTypeForMessage(message.GetType());
        var middleware = GetPublishMiddlewareForMessage(publicationType);
        if (middleware != null)
        {
            var context = new Messaging.Middleware.PublishContext(message, metadata ?? new PublishMetadata());
            await middleware.RunAsync(context, async ct =>
            {
                var publisher = _publishersByType[publicationType];
                await PublishAsync(publisher, message, context.Metadata, 0, ct)
                    .ConfigureAwait(false);
                return true;
            }, cancellationToken).ConfigureAwait(false);
            return;
        }

        var pub = _publishersByType[publicationType];
        await PublishAsync(pub, message, metadata, 0, cancellationToken)
            .ConfigureAwait(false);
    }

    private Type GetPublicationTypeForMessage(Type messageType)
    {
        if (_publishersByType.Count == 0)
        {
            _log.LogError("Error publishing message, no publishers registered. Has the bus been started?");
            throw new InvalidOperationException("Error publishing message, no publishers registered. Has the bus been started?");
        }

        if (!TryGetPublicationType(_publishersByType, messageType, out var publicationType))
        {
            if (messageType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(messageType))
            {
                // A v8 batch call, PublishAsync(messages), still compiles because a collection is
                // itself a valid single message, so say what to do instead.
                const string batchHint =
                    "To publish each item in a collection as a batch, call " + nameof(PublishBatchAsync) + " instead of " + nameof(PublishAsync) + ".";

                _log.LogError(
                    "Error publishing message. No publishers registered for message type '{MessageType}'. " + batchHint,
                    messageType);
                throw new InvalidOperationException(
                    $"Error publishing message, no publishers registered for message type '{messageType}'. {batchHint}");
            }

            _log.LogError(
                "Error publishing message. No publishers registered for message type '{MessageType}'.",
                messageType);
            throw new InvalidOperationException(
                $"Error publishing message, no publishers registered for message type '{messageType}'.");
        }

        return publicationType;
    }

    /// <summary>
    /// Finds the message type of the publication to use for a message of <paramref name="messageType"/>:
    /// the publication registered for that exact type, otherwise the one registered for its closest
    /// base class, otherwise the one registered for an interface it implements. The message is then
    /// serialized by that publication's serializer, as the registered type.
    /// </summary>
    private static bool TryGetPublicationType<TPublisher>(
        Dictionary<Type, TPublisher> publishers,
        Type messageType,
        out Type publicationType)
    {
        for (var type = messageType; type != null; type = type.BaseType)
        {
            if (publishers.ContainsKey(type))
            {
                publicationType = type;
                return true;
            }
        }

        // Check the registered interfaces rather than asking the message type for its own, so the
        // lookup doesn't depend on interface metadata that trimming may remove.
        var interfaces = publishers.Keys.Where(type => type.IsInterface && type.IsAssignableFrom(messageType)).ToList();
        if (interfaces.Count > 1)
        {
            throw new InvalidOperationException(
                $"Error publishing message of type '{messageType}': it implements more than one interface with a registered publication " +
                $"({string.Join(", ", interfaces.Select(type => $"'{type}'"))}), so the publication to use is ambiguous. " +
                $"Register a publication for '{messageType}' or one of its base classes instead.");
        }

        publicationType = interfaces.Count == 1 ? interfaces[0] : null;
        return publicationType != null;
    }

    private async Task PublishAsync<TMessage>(
        IMessagePublisher publisher,
        TMessage message,
        PublishMetadata metadata,
        int attemptCount,
        CancellationToken cancellationToken) where TMessage : class
    {
        attemptCount++;

        var isFirstAttempt = attemptCount == 1;
        Activity activity = null;
        Stopwatch publishWatch = null;
        var messageType = message.GetType();

        if (isFirstAttempt)
        {
            activity = JustSayingDiagnostics.ActivitySource.StartActivity(
                $"{messageType.Name} publish",
                ActivityKind.Producer);

            if (activity is not null)
            {
                activity.SetTag("messaging.operation.name", "publish");
                activity.SetTag("messaging.operation.type", "send");
                activity.SetTag("messaging.message.id", MessageIdentity.GetId(message, MessageMetadataProvider));
                activity.SetTag("messaging.message.type", messageType.FullName);
            }

            publishWatch = Stopwatch.StartNew();
        }

        try
        {
            using (_monitor.MeasurePublish())
            {
                await publisher.PublishAsync(message, metadata, cancellationToken)
                    .ConfigureAwait(false);
            }

            JustSayingDiagnostics.ClientSentMessages.Add(1);
        }
        catch (Exception ex)
        {
            if (attemptCount >= Config.PublishFailureReAttempts || !IsRetryablePublishFailure(ex, cancellationToken))
            {
                _monitor.IssuePublishingMessage();

                if (activity is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity.AddEvent(new ActivityEvent("exception",
                        tags: new ActivityTagsCollection
                        {
                            { "exception.type", ex.GetType().FullName },
                            { "exception.message", ex.Message },
                            { "exception.stacktrace", ex.ToString() },
                        }));
                }

                JustSayingDiagnostics.ClientSentMessages.Add(1,
                    new KeyValuePair<string, object>("error.type", ex.GetType().FullName));

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
        finally
        {
            if (isFirstAttempt)
            {
                publishWatch.Stop();
                JustSayingDiagnostics.ClientOperationDuration.Record(
                    publishWatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object>("messaging.operation.type", "send"));
                activity?.Dispose();
            }
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
    public async Task PublishBatchAsync<TMessage>(IEnumerable<TMessage> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken) where TMessage : class
    {
        if (messages == null) throw new ArgumentNullException(nameof(messages));

        EnsureStarted();

        var messageList = messages.ToList();
        if (messageList.Count == 0)
        {
            return;
        }

        if (messageList.Contains(null))
        {
            throw new ArgumentException("The batch cannot contain a null message.", nameof(messages));
        }

        // Route by each message's runtime type, so a single batch may contain more than one message
        // type, each fanned out to the publication found for it (see TryGetPublicationType).
        // Middleware is resolved per publication rather than for the batch as a whole, so per-type
        // middleware only ever sees the messages it was registered for.
        var tasks = new List<Task>();
        foreach (var group in messageList.GroupBy(message => GetBatchPublicationTypeForMessage(message.GetType())))
        {
            tasks.Add(PublishGroupAsync(group.Key, group.ToList(), metadata, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task PublishGroupAsync<TMessage>(
        Type messageType,
        List<TMessage> group,
        PublishBatchMetadata metadata,
        CancellationToken cancellationToken) where TMessage : class
    {
        var middleware = GetPublishMiddlewareForMessage(messageType);

        if (middleware != null)
        {
            var context = new Messaging.Middleware.PublishContext(group, metadata ?? new PublishBatchMetadata());
            await middleware.RunAsync(context, async ct =>
            {
                var publisher = _batchPublishersByType[messageType];
                await PublishAsync(publisher, group, (PublishBatchMetadata)context.Metadata, 0, messageType, ct)
                    .ConfigureAwait(false);
                return true;
            }, cancellationToken).ConfigureAwait(false);
            return;
        }

        var batchPublisher = _batchPublishersByType[messageType];
        await PublishAsync(batchPublisher, group, metadata, 0, messageType, cancellationToken).ConfigureAwait(false);
    }

    private Type GetBatchPublicationTypeForMessage(Type messageType)
    {
        if (_publishersByType.Count == 0)
        {
            const string errorMessage = "Error publishing message batch, no publishers registered. Has the bus been started?";
            _log.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        if (!TryGetPublicationType(_batchPublishersByType, messageType, out var publicationType))
        {
            _log.LogError("Error publishing message batch. No publishers registered for message type '{MessageType}'.", messageType);
            throw new InvalidOperationException($"Error publishing message batch, no publishers registered for message type '{messageType}'.");
        }

        return publicationType;
    }

    private PublishMessageMiddleware GetPublishMiddlewareForMessage(Type messageType)
    {
        if (_publishMiddlewareByType.TryGetValue(messageType, out var perType))
        {
            return perType;
        }

        return PublishMiddleware;
    }

    private async Task PublishAsync<TMessage>(
        IMessageBatchPublisher publisher,
        List<TMessage> messages,
        PublishBatchMetadata metadata,
        int attemptCount,
        Type messageType,
        CancellationToken cancellationToken) where TMessage : class
    {
        var batchSize = metadata?.BatchSize ?? 10;
        batchSize = Math.Min(batchSize, 10);
        attemptCount++;

        var isFirstAttempt = attemptCount == 1;
        Activity activity = null;
        Stopwatch publishWatch = null;

        if (isFirstAttempt)
        {
            activity = JustSayingDiagnostics.ActivitySource.StartActivity(
                $"{messageType.Name} publish",
                ActivityKind.Producer);

            if (activity is not null)
            {
                activity.SetTag("messaging.operation.name", "publish");
                activity.SetTag("messaging.operation.type", "send");
                activity.SetTag("messaging.message.type", messageType.FullName);
                activity.SetTag("messaging.batch.message_count", messages.Count);
            }

            publishWatch = Stopwatch.StartNew();
        }

        try
        {
            foreach (var chunk in messages.Chunk(batchSize))
            {
                try
                {
                    using (_monitor.MeasurePublish())
                    {
                        await publisher.PublishBatchAsync(chunk, metadata, cancellationToken).ConfigureAwait(false);
                    }

                    JustSayingDiagnostics.ClientSentMessages.Add(chunk.Length);
                }
                catch (Exception ex)
                {
                    if (attemptCount >= PublishBatchConfiguration.PublishFailureReAttempts || !IsRetryablePublishFailure(ex, cancellationToken))
                    {
                        _monitor.IssuePublishingMessage();

                        if (activity is not null)
                        {
                            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                            activity.AddEvent(new ActivityEvent("exception",
                                tags: new ActivityTagsCollection
                                {
                                    { "exception.type", ex.GetType().FullName },
                                    { "exception.message", ex.Message },
                                    { "exception.stacktrace", ex.ToString() },
                                }));
                        }

                        JustSayingDiagnostics.ClientSentMessages.Add(chunk.Length,
                            new KeyValuePair<string, object>("error.type", ex.GetType().FullName));

                        _log.LogError(
                            ex,
                            "Failed to publish a message batch of type '{MessageType}'. Halting after attempt number {PublishAttemptCount}.",
                            messageType,
                            attemptCount);

                        throw;
                    }

                    _log.LogWarning(
                        ex,
                        "Failed to publish a message batch of type '{MessageType}'. Retrying after attempt number {PublishAttemptCount} of {PublishFailureReattempts}.",
                        messageType,
                        attemptCount,
                        PublishBatchConfiguration.PublishFailureReAttempts);

                    var delayForAttempt = TimeSpan.FromMilliseconds(Config.PublishFailureBackoff.TotalMilliseconds * attemptCount);
                    await Task.Delay(delayForAttempt, cancellationToken).ConfigureAwait(false);

                    await PublishAsync(publisher, messages, metadata, attemptCount, messageType, cancellationToken).ConfigureAwait(false);
                }
            }

        }
        finally
        {
            if (isFirstAttempt)
            {
                publishWatch.Stop();
                JustSayingDiagnostics.ClientOperationDuration.Record(
                    publishWatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object>("messaging.operation.type", "send"));
                activity?.Dispose();
            }
        }
    }

    private static bool IsRetryablePublishFailure(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        // A message that can't be serialized (an unsupported type, missing source-generated metadata, a cycle,
        // NaN and similar) fails the same way every time, so retrying only delays the error.
        return exception is not (System.Text.Json.JsonException
            or Newtonsoft.Json.JsonException
            or NotSupportedException
            or ArgumentException);
    }

    private void EnsureStarted()
    {
        if (!_busStarted && _startupTasks.Count > 0)
        {
            throw new InvalidOperationException($"There are pending startup tasks that must be executed by calling {nameof(StartAsync)} before messages may be published.");
        }
    }
}
