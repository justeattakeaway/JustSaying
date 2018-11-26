using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
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
            Action<Exception, Amazon.SQS.Model.Message> onError = null,
            IMessageLockAsync messageLock = null,
            IMessageBackoffStrategy messageBackoffStrategy = null)
        {
            _queue = queue;
            _messagingMonitor = messagingMonitor;
            onError = onError ?? ((ex, message) => { });
            _log = loggerFactory.CreateLogger("JustSaying");

            _messageProcessingStrategy = new DefaultThrottledThroughput(_messagingMonitor);
            _messageHandlerWrapper = new MessageHandlerWrapper(messageLock, _messagingMonitor);
            _messageDispatcher = new MessageDispatcher(queue, serializationRegister, messagingMonitor, onError, _handlerMap, loggerFactory, messageBackoffStrategy);

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
            var queue = _queue.QueueName;
            var region = _queue.Region.SystemName;
            var queueInfo = $"Queue: {queue}, Region: {region}";

            // Run task in background
            // ListenLoop will cancel gracefully, so no need to pass cancellation token to Task.Run
            _ = Task.Run(async () =>
            {
                await ListenLoop(cancellationToken).ConfigureAwait(false);
                IsListening = false;
                _log.LogInformation($"Stopped Listening - {queueInfo}");
            });

            IsListening = true;
            _log.LogInformation($"Starting Listening - {queueInfo}");
        }

        internal async Task ListenLoop(CancellationToken ct)
        {
            var queueName = _queue.QueueName;
            var region = _queue.Region.SystemName;
            ReceiveMessageResponse sqsMessageResponse = null;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    sqsMessageResponse = await GetMessagesFromSqsQueue(queueName, region, ct).ConfigureAwait(false);
                    var messageCount = sqsMessageResponse.Messages.Count;

                    _log.LogTrace(
                        $"Polled for messages - Queue: {queueName}, Region: {region}, MessageCount: {messageCount}");
                }
                catch (InvalidOperationException ex)
                {
                    _log.LogTrace(
                        $"Could not determine number of messages to read from [{queueName}], region: [{region}]. Ex: {ex}");
                }
                catch (OperationCanceledException ex)
                {
                    _log.LogTrace($"Suspected no message in queue [{queueName}], region: [{region}]. Ex: {ex}");
                }
                catch (Exception ex)
                {
                    _log.LogError(0, ex, $"Issue receiving messages for queue {queueName}, region {region}");
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
                    _log.LogError(0, ex, $"Issue in message handling loop for queue {queueName}, region {region}");
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
                        _log.LogInformation($"Receiving messages from queue {queueName}, region {region}, timed out");
                    }
                }

                watch.Stop();

                _messagingMonitor.ReceiveMessageTime(watch.ElapsedMilliseconds, queueName, region);

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
    }
}
