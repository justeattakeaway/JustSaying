using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        int _maxVisibilityTimeoutRenewals;

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

            _messageProcessingStrategy = new DefaultThrottledThroughput(_messagingMonitor, _log);
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
        public SqsNotificationListener WithMaximumConcurrentLimitOnMessagesInFlightOf(
            int maximumAllowedMessagesInflight,
            TimeSpan? startTimeout = null)
        {
            var options = new ThrottledOptions()
            {
                MaxConcurrency = maximumAllowedMessagesInflight,
                StartTimeout = startTimeout ?? Timeout.InfiniteTimeSpan,
                Logger = _log,
                MessageMonitor = _messagingMonitor,
            };

            _messageProcessingStrategy = new Throttled(options);

            return this;
        }

        public SqsNotificationListener WithMaxVisibilityTimeoutRenewals(int maxVisibilityTimeoutRenewals)
        {
            _maxVisibilityTimeoutRenewals = maxVisibilityTimeoutRenewals;
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
                await ListenLoopAsync(cancellationToken).ConfigureAwait(false);
                IsListening = false;
                _log.LogInformation("Stopped listening on queue '{QueueName}' in region '{Region}'.", queueName, region);
            });

            IsListening = true;
            _log.LogInformation("Starting listening on queue '{QueueName}' in region '{Region}'.", queueName, region);
        }

        private async Task ListenLoopAsync(CancellationToken ct)
        {
            var queueName = _queue.QueueName;
            var regionName = _queue.Region.SystemName;
            ReceiveMessageResponse sqsMessageResponse = null;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    sqsMessageResponse = await GetMessagesFromSqsQueueAsync(queueName, regionName, ct).ConfigureAwait(false);

                    int messageCount = sqsMessageResponse?.Messages?.Count ?? 0;

                    _log.LogTrace(
                        "Polled for messages on queue '{QueueName}' in region '{Region}', and received {MessageCount} messages.",
                        queueName,
                        regionName,
                        messageCount);
                }
                catch (InvalidOperationException ex)
                {
                    _log.LogTrace(
                        ex,
                        "Could not determine number of messages to read from queue '{QueueName}' in '{Region}'.",
                        queueName,
                        regionName);
                }
                catch (OperationCanceledException ex)
                {
                    _log.LogTrace(
                        ex,
                        "Suspected no message on queue '{QueueName}' in region '{Region}'.",
                        queueName,
                        regionName);
                }
#pragma warning disable CA1031
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    _log.LogError(
                        ex,
                        "Error receiving messages on queue '{QueueName}' in region '{Region}'.",
                        queueName,
                        regionName);
                }

                if (sqsMessageResponse == null || sqsMessageResponse.Messages.Count < 1)
                {
                    continue;
                }

                try
                {
                    var batchInflightTracker = new MessageBatchInflightTracker(sqsMessageResponse.Messages);

                    StartBatchHeartbeat(batchInflightTracker, ct);

                    foreach (var message in sqsMessageResponse.Messages)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            return;
                        }

                        if (!await TryHandleMessageAsync(message, batchInflightTracker, ct).ConfigureAwait(false))
                        {
                            // No worker free to process any messages
                            _log.LogWarning(
                                "Unable to process message with Id {MessageId} for queue '{QueueName}' in region '{Region}' as no workers are available.",
                                message.MessageId,
                                queueName,
                                regionName);
                        }
                    }
                }
#pragma warning disable CA1031
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    _log.LogError(
                        ex,
                        "Error in message handling loop for queue '{QueueName}' in region '{Region}'.",
                        queueName,
                        regionName);
                }
            }
        }

        /// <summary>
        /// Creates a task that extends the message visibility of the batch on a cadence of half the timeout duration, until all of the messages have been processed.
        /// </summary>
        /// <param name="inflightTracker"></param>
        /// <param name="cancellationToken"></param>
        private void StartBatchHeartbeat(MessageBatchInflightTracker inflightTracker, CancellationToken cancellationToken)
        {
            if (_maxVisibilityTimeoutRenewals == 0)
            {
                return;
            }
            _ = Task.Run(async () =>
            {
                int timeoutRenewalsCount = 0;
                while (!cancellationToken.IsCancellationRequested && timeoutRenewalsCount < _maxVisibilityTimeoutRenewals)
                {
                    timeoutRenewalsCount++;
                    try
                    {
                        await Task.Delay(TimeSpan.FromTicks(_queue.VisibilityTimeout.Ticks / 2), cancellationToken)
                            .ConfigureAwait(false);

                        var inflightMessages = inflightTracker.InflightMessages.ToList();
                        if (inflightMessages.Count == 0)
                        {
                            return;
                        }

                        await _queue.Client.ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest
                        {
                            QueueUrl = _queue.Uri.AbsoluteUri,
                            Entries = inflightMessages.Select(m =>
                                new ChangeMessageVisibilityBatchRequestEntry
                                {
                                    Id = m.MessageId,
                                    ReceiptHandle = m.ReceiptHandle,
                                    VisibilityTimeout = (int) _queue.VisibilityTimeout.TotalSeconds
                                }).ToList()
                        }, cancellationToken).ConfigureAwait(false);
                    }
#pragma warning disable CA1031
                    catch (Exception)
#pragma warning restore CA1031
                    {
                        // Heartbeat should continue until cancellated
                    }
                }
            });
        }

        private async Task<ReceiveMessageResponse> GetMessagesFromSqsQueueAsync(string queueName, string region, CancellationToken ct)
        {
            int maxNumberOfMessages = await GetDesiredNumberOfMessagesToRequestFromSqsAsync()
                .ConfigureAwait(false);

            if (maxNumberOfMessages < 1)
            {
                return null;
            }

            var request = new ReceiveMessageRequest
            {
                QueueUrl = _queue.Uri.AbsoluteUri,
                MaxNumberOfMessages = maxNumberOfMessages,
                WaitTimeSeconds = 20,
                AttributeNames = _requestMessageAttributeNames
            };

            using (var receiveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(300)))
            {
                ReceiveMessageResponse sqsMessageResponse;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

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

                stopwatch.Stop();

                _messagingMonitor.ReceiveMessageTime(stopwatch.Elapsed, queueName, region);

                return sqsMessageResponse;
            }
        }

        private async Task<int> GetDesiredNumberOfMessagesToRequestFromSqsAsync()
        {
            int maximumMessagesFromAws = MessageConstants.MaxAmazonMessageCap;
            int maximumWorkers = _messageProcessingStrategy.MaxConcurrency;

            int messagesToRequest = Math.Min(maximumWorkers, maximumMessagesFromAws);

            if (messagesToRequest < 1)
            {
                // Wait for the strategy to have at least one worker available
                int availableWorkers = await _messageProcessingStrategy.WaitForAvailableWorkerAsync().ConfigureAwait(false);

                messagesToRequest = Math.Min(availableWorkers, maximumMessagesFromAws);
            }

            if (messagesToRequest < 1)
            {
                _log.LogWarning("No workers are available to process SQS messages.");
                messagesToRequest = 0;
            }

            return messagesToRequest;
        }

        private Task<bool> TryHandleMessageAsync(Amazon.SQS.Model.Message message, MessageBatchInflightTracker inflightTracker, CancellationToken ct)
        {
            async Task DispatchAsync()
            {
                await _messageDispatcher.DispatchMessage(message, inflightTracker, ct);
            }

            return _messageProcessingStrategy.StartWorkerAsync(DispatchAsync, ct);
        }

        public ICollection<ISubscriber> Subscribers { get; }

        private static void DefaultErrorHandler(Exception exception, Amazon.SQS.Model.Message message)
        {
            // No-op
        }
    }
}
