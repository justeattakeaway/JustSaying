using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    internal class MessageReceiver : IMessageReceiver
    {
        private readonly ISqsQueue _queue;
        private readonly IMessageMonitor _messagingMonitor;
        private readonly ILogger _logger;
        private readonly List<string> _requestMessageAttributeNames = new List<string>();

        public MessageReceiver(
            ISqsQueue queue,
            IMessageMonitor messagingMonitor,
            ILogger<MessageReceiver> logger,
            IMessageBackoffStrategy messageBackoffStrategy)
        {
            _queue = queue;
            _messagingMonitor = messagingMonitor;
            _logger = logger;

            if (messageBackoffStrategy != null)
            {
                _requestMessageAttributeNames.Add(MessageSystemAttributeName.ApproximateReceiveCount);
            }
        }

        public string QueueName => _queue?.QueueName;
        public string Region => _queue?.Region?.SystemName;

        public async Task<ReceiveMessageResponse> GetMessagesAsync(int maxNumberOfMessages, CancellationToken ct)
        {
            // todo: configurable wait time?
            var request = new ReceiveMessageRequest
            {
                QueueUrl = _queue.Uri.AbsoluteUri,
                MaxNumberOfMessages = maxNumberOfMessages,
                WaitTimeSeconds = 20,
                AttributeNames = _requestMessageAttributeNames
            };

            ReceiveMessageResponse sqsMessageResponse;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            sqsMessageResponse = await TryGetMessages(request, ct).ConfigureAwait(false);

            stopwatch.Stop();

            _messagingMonitor.ReceiveMessageTime(stopwatch.Elapsed, QueueName, Region);

            return sqsMessageResponse;
        }

        private async Task<ReceiveMessageResponse> TryGetMessages(ReceiveMessageRequest request, CancellationToken ct)
        {
            ReceiveMessageResponse sqsMessageResponse = null;

            using var receiveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(300));

            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, receiveTimeout.Token);

                sqsMessageResponse = await _queue.Client.ReceiveMessageAsync(request, linkedCts.Token).ConfigureAwait(false);

                int messageCount = sqsMessageResponse?.Messages?.Count ?? 0;

                _logger.LogTrace(
                    "Polled for messages on queue '{QueueName}' in region '{Region}', and received {MessageCount} messages.",
                    QueueName,
                    Region,
                    messageCount);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogTrace(
                    ex,
                    "Could not determine number of messages to read from queue '{QueueName}' in '{Region}'.",
                    QueueName,
                    Region);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogTrace(
                    ex,
                    "Suspected no message on queue '{QueueName}' in region '{Region}'.",
                    QueueName,
                    Region);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(
                    ex,
                    "Error receiving messages on queue '{QueueName}' in region '{Region}'.",
                    QueueName,
                    Region);
            }
            finally
            {
                if (receiveTimeout.Token.IsCancellationRequested)
                {
                    _logger.LogInformation("Timed out while receiving messages from queue '{QueueName}' in region '{Region}'.",
                        QueueName, Region);
                }
            }

            return sqsMessageResponse;
        }
    }
}
