using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SqsNotificationListener : INotificationSubscriber
    {
        private readonly IMessageMonitor _messagingMonitor;

        private readonly MessageHandlerWrapper _messageHandlerWrapper;
        private readonly HandlerMap _handlerMap = new HandlerMap();
        private readonly IMessageCoordinator _listener;

        private readonly ILogger _log;

        public bool IsListening { get; private set; }

        public SqsNotificationListener(
            ISqsQueue queue,
            IMessageSerializationRegister serializationRegister,
            IMessageMonitor messagingMonitor,
            ILoggerFactory loggerFactory,
            IMessageContextAccessor messageContextAccessor,
            Action<Exception, Amazon.SQS.Model.Message> onError = null,
            IMessageLockAsync messageLock = null,
            IMessageBackoffStrategy messageBackoffStrategy = null)
        {
            _messagingMonitor = messagingMonitor;
            onError ??= DefaultErrorHandler;
            _log = loggerFactory.CreateLogger("JustSaying");

            // todo: strategy factory?
#pragma warning disable CA2000 // Dispose objects before losing scope
            var messageProcessingStrategy = new DefaultThrottledThroughput(_messagingMonitor, _log);
#pragma warning restore CA2000 // Dispose objects before losing scope
            _messageHandlerWrapper = new MessageHandlerWrapper(messageLock, _messagingMonitor, loggerFactory);

            var messageDispatcher = new MessageDispatcher(
                queue,
                serializationRegister,
                messagingMonitor,
                onError,
                _handlerMap,
                loggerFactory,
                messageBackoffStrategy,
                messageContextAccessor);

            var messageRequester = new MessageRequester(
                queue,
                messagingMonitor,
                loggerFactory.CreateLogger<MessageRequester>(),
                messageBackoffStrategy);

            _listener = new MessageCoordinator(_log, messageRequester, messageDispatcher, messageProcessingStrategy);

            Subscribers = new Collection<ISubscriber>();
        }

        public string Queue => _listener.QueueName;

        // ToDo: This should not be here.
        public SqsNotificationListener WithMaximumConcurrentLimitOnMessagesInFlightOf(
            int maximumAllowedMesagesInFlight,
            TimeSpan? startTimeout = null)
        {
            var options = new ThrottledOptions()
            {
                MaxConcurrency = maximumAllowedMesagesInFlight,
                StartTimeout = startTimeout ?? Timeout.InfiniteTimeSpan,
                Logger = _log,
                MessageMonitor = _messagingMonitor,
            };

#pragma warning disable CA2000 // Dispose objects before losing scope
            _listener.WithMessageProcessingStrategy(new Throttled(options));
#pragma warning restore CA2000 // Dispose objects before losing scope

            return this;
        }

        public SqsNotificationListener WithMessageProcessingStrategy(IMessageProcessingStrategy messageProcessingStrategy)
        {
            _listener.WithMessageProcessingStrategy(messageProcessingStrategy);
            return this;
        }

        public void AddMessageHandler<T>(Func<IHandlerAsync<T>> futureHandler) where T : Message
        {
            if (_handlerMap.ContainsKey(typeof(T)))
            {
                throw new NotSupportedException(
                    $"The handler for '{typeof(T)}' messages on this queue has already been registered.");
            }

            Subscribers.Add(new Subscriber(typeof(T)));

            var handlerFunc = _messageHandlerWrapper.WrapMessageHandler(futureHandler);
            _handlerMap.Add(typeof(T), handlerFunc);
        }

        public void Listen(CancellationToken cancellationToken)
        {
            var queueName = _listener.QueueName;
            var region = _listener.Region;

            // Run task in background
            // ListenLoop will cancel gracefully, so no need to pass cancellation token to Task.Run
            _ = Task.Run(async () =>
            {
                await _listener.ListenAsync(cancellationToken).ConfigureAwait(false);
                IsListening = false;
                _log.LogInformation("Stopped listening on queue '{QueueName}' in region '{Region}'.", queueName, region);
            });

            IsListening = true;
            _log.LogInformation("Starting listening on queue '{QueueName}' in region '{Region}'.", queueName, region);
        }

        public ICollection<ISubscriber> Subscribers { get; }

        private static void DefaultErrorHandler(Exception exception, Amazon.SQS.Model.Message message)
        {
            // No-op
        }
    }
}
