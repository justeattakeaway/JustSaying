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
        private readonly MessageDispatcher _messageDispatcher;
        private readonly IMessageCoordinator _listener;

        private readonly ILogger _log;

        public bool IsListening { get; private set; }

        public SqsNotificationListener(
            ISqsQueue queue,
            IMessageSerializationRegister serializationRegister,
            IMessageMonitor messagingMonitor,
            ILoggerFactory loggerFactory,
            IMessageContextAccessor messageContextAccessor,
            IMessageProcessingStrategy messageProcessingStrategy,
            Action<Exception, Amazon.SQS.Model.Message> onError = null,
            IMessageLockAsync messageLock = null,
            IMessageBackoffStrategy messageBackoffStrategy = null)
        {
            onError ??= DefaultErrorHandler;
            _log = loggerFactory.CreateLogger("JustSaying");

            _messageDispatcher = new MessageDispatcher(
                queue,
                serializationRegister,
                messagingMonitor,
                onError,
                loggerFactory,
                messageBackoffStrategy,
                messageContextAccessor,
                messageLock);

            var messageReceiver = new MessageReceiver(
                queue,
                messagingMonitor,
                loggerFactory.CreateLogger<MessageReceiver>(),
                messageBackoffStrategy);

            _listener = new MessageCoordinator(_log, messageReceiver, _messageDispatcher, messageProcessingStrategy);

            Subscribers = new Collection<ISubscriber>();
        }

        public string Queue => _listener.QueueName;

        // todo: remove?
        public SqsNotificationListener WithMessageProcessingStrategy(IMessageProcessingStrategy messageProcessingStrategy)
        {
            _listener.WithMessageProcessingStrategy(messageProcessingStrategy);
            return this;
        }

        public void AddMessageHandler<T>(Func<IHandlerAsync<T>> futureHandler) where T : Message
        {
            if (_messageDispatcher.AddMessageHandler(futureHandler))
            {
                Subscribers.Add(new Subscriber(typeof(T)));
            }
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
