using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using NLog;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SqsNotificationListener : INotificationSubscriber
    {
        private readonly SqsQueueBase _queue;
        private readonly IMessageMonitor _messagingMonitor;

        private readonly MessageDispatcher _messageDispatcher;
        private readonly MessageHandlerWrapper _messageHandlerWrapper;
        private IMessageProcessingStrategy _messageProcessingStrategy;
        private readonly HandlerMap _handlerMap = new HandlerMap();

        private CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        public SqsNotificationListener(
            SqsQueueBase queue,
            IMessageSerialisationRegister serialisationRegister,
            IMessageMonitor messagingMonitor,
            Action<Exception, Amazon.SQS.Model.Message> onError = null,
            IMessageLock messageLock = null)
        {
            _queue = queue;
            _messagingMonitor = messagingMonitor;
            onError = onError ?? ((ex,message) => { });
            
            _messageProcessingStrategy = new DefaultThrottledThroughput(_messagingMonitor);
            _messageHandlerWrapper = new MessageHandlerWrapper(messageLock, _messagingMonitor);
            _messageDispatcher = new MessageDispatcher(queue, serialisationRegister, messagingMonitor, onError, _handlerMap);

            Subscribers = new Collection<ISubscriber>();
        }

        public string Queue
        {
            get { return _queue.QueueName; }
        }

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
            Subscribers.Add(new Subscriber(typeof(T)));

            var handlerFunc = _messageHandlerWrapper.WrapMessageHandler(futureHandler);
            _handlerMap.Add(typeof(T), handlerFunc);
        }

        public void Listen()
        {
            var queue = _queue.QueueName;
            var region = _queue.Region.SystemName;
            var queueInfo = string.Format("Queue: {0}, Region: {1}", queue, region);

            _cts = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        await ListenLoop(_cts.Token);
                    }
                })
                .Unwrap()
                .ContinueWith(t => LogTaskEndState(t, queueInfo));

            Log.Info(
                "Starting Listening - {0}", 
                queueInfo);
        }

        private static void LogTaskEndState(Task task, string queueInfo)
        {
            if (task.IsFaulted)
            {
                Log.Warn(
                    "[Faulted] Stopped Listening - {0}\n{1}",
                     queueInfo,
                     AggregateExceptionDetails(task.Exception));
            }
            else
            {
                var endState = task.Status.ToString();
                Log.Info(
                    "[{0}] Stopped Listening - {1}",
                    endState,
                    queueInfo);
            }
        }

        private static string AggregateExceptionDetails(AggregateException ex)
        {
            var flatEx = ex.Flatten();

            if (flatEx.InnerExceptions.Count == 0)
            {
                return "AggregateException containing no inner exceptions\n" + ex;
            }

            if (flatEx.InnerExceptions.Count == 1)
            {
                return ex.InnerExceptions[0].ToString();
            }

            var innerExDetails = new StringBuilder();
            innerExDetails.AppendFormat("AggregateException containing {0} inner exceptions", flatEx.InnerExceptions.Count);
            foreach (var innerEx in flatEx.InnerExceptions)
            {
                innerExDetails.AppendLine(innerEx.ToString());
            }

            return innerExDetails.ToString();
        }

       public void StopListening()
        {
            _cts.Cancel();
            Log.Info(
                "Stopping Listening - Queue: {0}, Region: {1}",
                _queue.QueueName,
                _queue.Region.SystemName);
        }

        internal async Task ListenLoop(CancellationToken ct)
        {
            var queueName = _queue.QueueName;
            var region = _queue.Region.SystemName;

            try
            {
                var numberOfMessagesToReadFromSqs = await GetNumberOfMessagesToReadFromSqs();

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                var request = new ReceiveMessageRequest
                    {
                        QueueUrl = _queue.Url,
                        MaxNumberOfMessages = numberOfMessagesToReadFromSqs,
                        WaitTimeSeconds = 20
                    };
                var sqsMessageResponse = await _queue.Client.ReceiveMessageAsync(request, ct);

                watch.Stop();

                _messagingMonitor.ReceiveMessageTime(watch.ElapsedMilliseconds);

                var messageCount = sqsMessageResponse.Messages.Count;

                Log.Trace(
                    "Polled for messages - Queue: {0}, Region: {1}, MessageCount: {2}",
                    queueName,
                    region,
                    messageCount);

                foreach (var message in sqsMessageResponse.Messages)
                {
                    HandleMessage(message);
                }
            }
            catch (InvalidOperationException ex)
            {
                Log.Trace(
                    "Suspected no message in queue [{0}], region: [{1}]. Ex: {2}",
                    queueName,
                    region,
                    ex);
            }
            catch (Exception ex)
            {
                var msg = string.Format(
                    "Issue in message handling loop for queue {0}, region {1}",
                    queueName,
                    region);
                Log.Error(ex, msg);
            }
        }

        private async Task<int> GetNumberOfMessagesToReadFromSqs()
        {
            var numberOfMessagesToreadFromSqs = Math.Min(_messageProcessingStrategy.AvailableMessageHandlers, MessageConstants.MaxAmazonMessageCap);

            if (numberOfMessagesToreadFromSqs == 0)
            {
                await _messageProcessingStrategy.AwaitAtLeastOneTaskToComplete();

                numberOfMessagesToreadFromSqs = Math.Min(_messageProcessingStrategy.AvailableMessageHandlers, MessageConstants.MaxAmazonMessageCap);
            }

            if (numberOfMessagesToreadFromSqs == 0)
            {
                throw new InvalidOperationException("Cannot determine numberOfMessagesToreadFromSqs");
            }

            return numberOfMessagesToreadFromSqs;
        }

        private void HandleMessage(Amazon.SQS.Model.Message message)
        {
            var action = new Func<Task>(() => _messageDispatcher.DispatchMessage(message));
            _messageProcessingStrategy.ProcessMessage(action);
        }

        public ICollection<ISubscriber> Subscribers { get; set; }
    }
}