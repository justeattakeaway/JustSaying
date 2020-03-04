using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels
{
    internal class MessageReceiveBuffer : IMessageReceiveBuffer
    {
        private readonly Channel<IQueueMessageContext> _channel;
        private readonly int _bufferLength;
        private readonly ISqsQueue _sqsQueue;
        private readonly IMessageMonitor _monitor;
        private readonly ILogger _logger;

        // todo: add logic around populating this
        private readonly List<string> _requestMessageAttributeNames = new List<string>();

        public ChannelReader<IQueueMessageContext> Reader => _channel.Reader;

        public MessageReceiveBuffer(
            int bufferLength,
            ISqsQueue sqsQueue,
            IMessageMonitor monitor,
            ILoggerFactory logger)
        {
            _bufferLength = bufferLength;
            _sqsQueue = sqsQueue;
            _monitor = monitor;
            _logger = logger.CreateLogger<IMessageReceiveBuffer>();
            _channel = Channel.CreateBounded<IQueueMessageContext>(bufferLength);
        }

        /// <summary>
        /// Starts the receive buffer until it's cancelled by the stopping token.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns>A task that completes when the cancellation token is fired.</returns>
        public async Task Run(CancellationToken stoppingToken)
        {
            await Task.Yield();

            ChannelWriter<IQueueMessageContext> writer = _channel.Writer;
            try
            {
                while (true)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    using (_monitor.MeasureThrottle())
                    {
                        var canWrite = await WaitToWriteAsync(writer, stoppingToken).ConfigureAwait(false);
                        if (!canWrite) break;
                    }

                    IList<Message> messages;
                    using (_monitor.MeasureReceive(_sqsQueue.QueueName, _sqsQueue.RegionSystemName))
                    {
                        messages = await GetMessagesAsync(_bufferLength, stoppingToken).ConfigureAwait(false);

                        if (messages == null) continue;
                    }

                    foreach (var message in messages)
                    {
                        IQueueMessageContext messageContext = _sqsQueue.ToMessageContext(message);
                        await writer.WriteAsync(messageContext).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _logger.LogInformation("Downloader for queue {QueueName} has completed, shutting down channel...",
                    _sqsQueue.Uri);
                writer.Complete();
                stoppingToken.ThrowIfCancellationRequested();
            }
        }

        private async Task<IList<Message>> GetMessagesAsync(int count, CancellationToken stoppingToken)
        {
            using var receiveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(300));
            IList<Message> messages;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using var linkedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, receiveTimeout.Token);

                messages = await _sqsQueue
                    .GetMessagesAsync(count, _requestMessageAttributeNames, stoppingToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (receiveTimeout.Token.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "Timed out while receiving messages from queue '{QueueName}' in region '{Region}'.",
                        _sqsQueue.QueueName, _sqsQueue.RegionSystemName);
                }
            }

            stopwatch.Stop();

            _monitor.ReceiveMessageTime(stopwatch.Elapsed, _sqsQueue.QueueName, _sqsQueue.RegionSystemName);

            return messages;
        }

        private async Task<bool> WaitToWriteAsync(ChannelWriter<IQueueMessageContext> writer,
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // we don't want to pass the stoppingToken here because
                    // we want to process any messages queued messages before stopping
                    using var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    using var linkedCts =
                        CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, stoppingToken);

                    bool writePermitted = await writer.WaitToWriteAsync(linkedCts.Token);
                    return writePermitted;
                }
                catch (OperationCanceledException)
                {
                    // no space in channel, check again
                    continue;
                }
            }

            return false;
        }
    }
}
