using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
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
        private readonly SqsQueueBase _queue;
        private readonly IMessageMonitor _messagingMonitor;
        private readonly List<string> _requestMessageAttributeNames = new List<string>();

        private readonly MessageDispatcher _messageDispatcher;
        private readonly MessageHandlerWrapper _messageHandlerWrapper;
        private IMessageProcessingStrategy _messageProcessingStrategy;
        private readonly HandlerMap _handlerMap = new HandlerMap();

        private readonly ILogger _log;

        public bool IsListening { get; private set; }

        public SqsNotificationListener(
            SqsQueueBase queue,
            IMessageSerializationRegister serializationRegister,
            IMessageMonitor messagingMonitor,
            ILoggerFactory loggerFactory,
            IMessageContextAccessor messageContextAccessor,
            Action<Exception, Amazon.SQS.Model.Message> onError = null,
            IMessageLockAsync messageLock = null,
            IMessageBackoffStrategy messageBackoffStrategy = null)
        {
            _queue = queue;
            _messagingMonitor = messagingMonitor;
            onError = onError ?? DefaultErrorHandler;
            _log = loggerFactory.CreateLogger("JustSaying");

            _messageProcessingStrategy = new DefaultThrottledThroughput(_messagingMonitor);
            _messageHandlerWrapper = new MessageHandlerWrapper(messageLock, _messagingMonitor);

            _messageDispatcher = new MessageDispatcher(
                _queue,
                serializationRegister,
                messagingMonitor,
                onError,
                _handlerMap,
                loggerFactory,
                messageBackoffStrategy,
                messageContextAccessor);

            Subscribers = new Collection<ISubscriber>();

            if (messageBackoffStrategy != null)
            {
                _requestMessageAttributeNames.Add(MessageSystemAttributeName.ApproximateReceiveCount);
            }
        }

        public string Queue => _queue.QueueName;

        // ToDo: This should not be here.
        public SqsNotificationListener WithMaximumConcurrentLimitOnMessagesInFlightOf(int maximumAllowedMesagesInFlight)
        {
            _messageProcessingStrategy = new Throttled(maximumAllowedMesagesInFlight, _messagingMonitor);
            return this;
        }

        public SqsNotificationListener WithMessageProcessingStrategy(IMessageProcessingStrategy messageProcessingStrategy)
        {
            _messageProcessingStrategy = messageProcessingStrategy;
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
            var queueName = _queue.QueueName;
            var region = _queue.Region.SystemName;

            // Run task in background
            // ListenLoop will cancel gracefully, so no need to pass cancellation token to Task.Run
            _ = Task.Run(async () =>
            {
                await ListenLoop(cancellationToken).ConfigureAwait(false);
                IsListening = false;
                _log.LogInformation("Stopped listening on queue '{QueueName}' in region '{Region}'.", queueName, region);
            });

            IsListening = true;
            _log.LogInformation("Starting listening on queue '{QueueName}' in region '{Region}'.", queueName, region);
        }

        internal async Task ListenLoop(CancellationToken ct)
        {
            var queueName = _queue.QueueName;
            var regionName = _queue.Region.SystemName;
            ReceiveMessageResponse sqsMessageResponse = null;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    sqsMessageResponse = await GetMessagesFromSqsQueue(queueName, regionName, ct).ConfigureAwait(false);
                    var messageCount = sqsMessageResponse.Messages.Count;

                    _log.LogTrace("Polled for messages on queue '{QueueName}' in region '{Region}', and received {MessageCount} messages.",
                        queueName, regionName, messageCount);
                }
                catch (InvalidOperationException ex)
                {
                    _log.LogTrace(0, ex, "Could not determine number of messages to read from queue '{QueueName}' in '{Region}'.",
                        queueName, regionName);
                }
                catch (OperationCanceledException ex)
                {
                    _log.LogTrace(0, ex, "Suspected no message on queue '{QueueName}', in region '{Region}'.",
                        queueName, regionName);
                }
                catch (Exception ex)
                {
                    _log.LogError(0, ex, "Error receiving messages on queue '{QueueName}' in region '{Region}'.",
                        queueName, regionName);
                }

                try
                {
                    if (sqsMessageResponse != null)
                    {
                        foreach (var message in sqsMessageResponse.Messages)
                        {
                            if (ct.IsCancellationRequested)
                            {
                                return;
                            }
                            await HandleMessage(message, ct).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(0, ex, "Error in message handling loop for queue '{QueueName}' in region '{Region}'.",
                        queueName, regionName);
                }
            }
        }

        private async Task<ReceiveMessageResponse> GetMessagesFromSqsQueue(string queueName, string region, CancellationToken ct)
        {
            var numberOfMessagesToReadFromSqs = await GetNumberOfMessagesToReadFromSqs()
                .ConfigureAwait(false);

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var request = new ReceiveMessageRequest
            {
                QueueUrl = _queue.Uri.AbsoluteUri,
                MaxNumberOfMessages = numberOfMessagesToReadFromSqs,
                WaitTimeSeconds = 20,
                AttributeNames = _requestMessageAttributeNames
            };

            using (var receiveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(300)))
            {
                ReceiveMessageResponse sqsMessageResponse;

                try
                {
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, receiveTimeout.Token))
                    {
                        sqsMessageResponse = await _queue.Client.ReceiveMessageAsync(request, linkedCts.Token)
                            .ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (receiveTimeout.Token.IsCancellationRequested)
                    {
                        _log.LogInformation("Timed out while receiving messages from queue '{QueueName}' in region '{Region}'.",
                            queueName, region);
                    }
                }

                watch.Stop();

                _messagingMonitor.ReceiveMessageTime(watch.Elapsed, queueName, region);

                return sqsMessageResponse;
            }
        }

        private async Task<int> GetNumberOfMessagesToReadFromSqs()
        {
            var numberOfMessagesToReadFromSqs = Math.Min(_messageProcessingStrategy.AvailableWorkers, MessageConstants.MaxAmazonMessageCap);

            if (numberOfMessagesToReadFromSqs == 0)
            {
                await _messageProcessingStrategy.WaitForAvailableWorkers().ConfigureAwait(false);

                numberOfMessagesToReadFromSqs = Math.Min(_messageProcessingStrategy.AvailableWorkers, MessageConstants.MaxAmazonMessageCap);
            }

            if (numberOfMessagesToReadFromSqs == 0)
            {
                throw new InvalidOperationException("Cannot determine numberOfMessagesToReadFromSqs");
            }

            return numberOfMessagesToReadFromSqs;
        }

        private Task HandleMessage(Amazon.SQS.Model.Message message, CancellationToken ct)
        {
            var action = new Func<Task>(() => _messageDispatcher.DispatchMessage(message, ct));
            return _messageProcessingStrategy.StartWorker(action, ct);
        }

        public ICollection<ISubscriber> Subscribers { get; set; }

        private static void DefaultErrorHandler(Exception exception, Amazon.SQS.Model.Message message)
        {
            // No-op
        }
    }
}
