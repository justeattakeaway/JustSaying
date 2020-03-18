using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying
{
    public sealed class JustSayingBus : IAmJustSaying, IAmJustInterrogating, IMessagingBus
    {
        private readonly Dictionary<string, Dictionary<string, ISqsQueue>> _subscribersByRegionAndQueue;
        private readonly Dictionary<string, Dictionary<Type, IMessagePublisher>> _publishersByRegionAndType;
        private readonly IList<ISqsQueue> _sqsQueues;

        private string _previousActiveRegion;

        public IMessagingConfig Config { get; private set; }

        private IMessageMonitor _monitor;
        public IMessageMonitor Monitor
        {
            get { return _monitor; }
            set { _monitor = value ?? new NullOpMessageMonitor(); }
        }

        private IConsumerBus ConsumerBus { get; set; }
        public IMessageSerializationRegister SerializationRegister { get; private set; }
        public IMessageLockAsync MessageLock
        {
            get => HandlerMap?.MessageLock;
            set => HandlerMap.MessageLock = value;
        }
        public IMessageContextAccessor MessageContextAccessor { get; set; }
        public HandlerMap HandlerMap { get; private set; }

        private IMessageBackoffStrategy _messageBackoffStrategy;
        private Action<Exception, SQSMessage> _onError;

        public void SetMessageBackoffStrategy(IMessageBackoffStrategy value)
        {
            _messageBackoffStrategy = value;
        }

        public void SetOnError(Action<Exception, SQSMessage> value)
        {
            _onError = value;
        }

        private readonly ILogger _log;

        private readonly object _syncRoot = new object();
        private readonly ICollection<IPublisher> _publishers;
        private ILoggerFactory _loggerFactory;

        public JustSayingBus(IMessagingConfig config, IMessageSerializationRegister serializationRegister, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _log = _loggerFactory.CreateLogger("JustSaying");

            Config = config;
            Monitor = new NullOpMessageMonitor();
            MessageContextAccessor = new MessageContextAccessor();

            _subscribersByRegionAndQueue = new Dictionary<string, Dictionary<string, ISqsQueue>>();
            _publishersByRegionAndType = new Dictionary<string, Dictionary<Type, IMessagePublisher>>();
            SerializationRegister = serializationRegister;
            _publishers = new HashSet<IPublisher>();

            _sqsQueues = new List<ISqsQueue>();

            HandlerMap = new HandlerMap(Monitor, _loggerFactory);
        }

        public void AddQueue(string region, ISqsQueue queue)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException(nameof(region));
            }

            _sqsQueues.Add(queue);
        }


        public void AddMessageHandler<T>(Func<IHandlerAsync<T>> futureHandler) where T : Message
        {
            HandlerMap.Add(futureHandler);
        }

        public void AddMessagePublisher<T>(IMessagePublisher messagePublisher, string region) where T : Message
        {
            if (Config.PublishFailureReAttempts == 0)
            {
                _log.LogWarning("You have not set a re-attempt value for publish failures. If the publish location is 'down' you may lose messages.");
            }

            if (!_publishersByRegionAndType.TryGetValue(region, out var publishersByType))
            {
                publishersByType = new Dictionary<Type, IMessagePublisher>();
                _publishersByRegionAndType.Add(region, publishersByType);
            }

            var topicType = typeof(T);
            _publishers.Add(new Publisher(topicType));

            publishersByType[topicType] = messagePublisher;
        }

        private Task _consumerCompletionTask;
        private bool _consumerStarted;
        public Task Start(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested) return Task.CompletedTask;

            // Double check lock to ensure single-start
            if (_consumerStarted) return _consumerCompletionTask;
            lock (_syncRoot)
            {
                if (_consumerStarted) return _consumerCompletionTask;

                _consumerCompletionTask = RunImpl(stoppingToken);

                _consumerStarted = true;
                return _consumerCompletionTask;
            }
        }

        private Task RunImpl(CancellationToken stoppingToken)
        {
            var dispatcher = new MessageDispatcher(
                SerializationRegister,
                Monitor,
                _onError,
                HandlerMap,
                _loggerFactory,
                _messageBackoffStrategy,
                MessageContextAccessor);

            ConsumerBus = new ConsumerGroup(
                _sqsQueues,
                Config.ConsumerConfig,
                dispatcher,
                Monitor,
                _loggerFactory);
            return ConsumerBus.Run(stoppingToken);
        }

        public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
        {
            var publisher = GetActivePublisherForMessage(message);
            await PublishAsync(publisher, message, metadata, 0, cancellationToken)
                .ConfigureAwait(false);
        }

        public IInterrogationResponse WhatDoIHave()
        {
            var handlers = HandlerMap.Types.Select(t => new Subscriber(t));
            return new InterrogationResponse(Config.Regions, handlers, _publishers);
        }

        private IMessagePublisher GetActivePublisherForMessage(Message message)
        {
            if (_publishersByRegionAndType.Count == 0)
            {
                var errorMessage = "Error publishing message, no publishers registered.";
                _log.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            string activeRegion = GetActiveRegionWithChangeLog();

            var publishersForRegionFound = _publishersByRegionAndType.TryGetValue(activeRegion, out var publishersForRegion);
            if (!publishersForRegionFound)
            {
                _log.LogError("Error publishing message. No publishers registered for active region '{Region}'.", activeRegion);
                throw new InvalidOperationException($"Error publishing message. No publishers registered for active region '{activeRegion}'.");
            }

            var messageType = message.GetType();
            var publisherFound = publishersForRegion.TryGetValue(messageType, out var publisher);

            if (!publisherFound)
            {
                _log.LogError(
                    "Error publishing message. No publishers registered for message type '{MessageType}' in active region '{Region}'.",
                    messageType,
                    activeRegion);

                throw new InvalidOperationException($"Error publishing message, no publishers registered for message type '{messageType}' in active region '{activeRegion}'.");
            }

            return publisher;
        }

        private string GetActiveRegionWithChangeLog()
        {
            string currentActiveRegion = GetActiveRegion();

            if (!string.Equals(_previousActiveRegion, currentActiveRegion, StringComparison.Ordinal))
            {
                if (_previousActiveRegion == null)
                {
                    _log.LogInformation("Active region for publishing has been initialized to '{Region}'.",
                        currentActiveRegion);
                }
                else
                {
                    _log.LogInformation("Active region for publishing has been changed to '{Region}', was '{PreviousRegion}'.",
                        currentActiveRegion, _previousActiveRegion);
                }

                _previousActiveRegion = currentActiveRegion;
            }

            return currentActiveRegion;
        }

        private string GetActiveRegion()
        {
            if (Config.GetActiveRegion != null)
            {
                return Config.GetActiveRegion();
            }

            return Config.Regions.First();
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
                using (Monitor.MeasurePublish())
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
                    Monitor.IssuePublishingMessage();

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

                var delayForAttempt = TimeSpan.FromMilliseconds(Config.PublishFailureBackoff.TotalMilliseconds * attemptCount);
                await Task.Delay(delayForAttempt, cancellationToken).ConfigureAwait(false);

                await PublishAsync(publisher, message, metadata, attemptCount, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
