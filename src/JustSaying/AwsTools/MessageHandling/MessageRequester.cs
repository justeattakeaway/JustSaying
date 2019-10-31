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
    // todo: internal?
    public class MessageRequester : IMessageRequester
    {
        private readonly ISqsQueue _queue;
        private readonly IMessageMonitor _messagingMonitor;
        private readonly ILogger<MessageRequester> _logger;
        private readonly List<string> _requestMessageAttributeNames = new List<string>();

        public MessageRequester(
            ISqsQueue queue,
            IMessageMonitor messagingMonitor,
            ILogger<MessageRequester> logger,
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

        public async Task<ReceiveMessageResponse> GetMessages(int maxNumberOfMessages, CancellationToken ct)
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = _queue.Uri.AbsoluteUri,
                MaxNumberOfMessages = maxNumberOfMessages,
                WaitTimeSeconds = 20,
                AttributeNames = _requestMessageAttributeNames
            };

            using var receiveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(300));
            ReceiveMessageResponse sqsMessageResponse;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, receiveTimeout.Token);

                sqsMessageResponse = await _queue.Client.ReceiveMessageAsync(request, linkedCts.Token).ConfigureAwait(false);
            }
            finally
            {
                if (receiveTimeout.Token.IsCancellationRequested)
                {
                    _logger.LogInformation("Timed out while receiving messages from queue '{QueueName}' in region '{Region}'.",
                        QueueName, Region);
                }
            }

            stopwatch.Stop();

            _messagingMonitor.ReceiveMessageTime(stopwatch.Elapsed, QueueName, Region);

            return sqsMessageResponse;
        }
    }
}
