using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels
{
    internal class MessageReceiveBuffer : IMessageReceiveBuffer
    {
        private readonly Channel<IQueueMessageContext> _channel;
        private readonly int _bufferLength;
        private readonly ISqsQueue _sqsQueue;
        private readonly MiddlewareBase<GetMessagesContext, IList<Message>> _sqsMiddleware;
        private readonly IMessageMonitor _monitor;
        private readonly ILogger _logger;

        private readonly List<string> _requestMessageAttributeNames = new List<string>();

        public ChannelReader<IQueueMessageContext> Reader => _channel.Reader;

        public MessageReceiveBuffer(
            int bufferLength,
            ISqsQueue sqsQueue,
            MiddlewareBase<GetMessagesContext, IList<Message>> sqsMiddleware,
            IMessageMonitor monitor,
            ILoggerFactory logger,
            IMessageBackoffStrategy messageBackoffStrategy = null)
        {
            _channel = Channel.CreateBounded<IQueueMessageContext>(bufferLength);
            _bufferLength = bufferLength;
            _sqsQueue = sqsQueue;
            _sqsMiddleware = sqsMiddleware;
            _monitor = monitor;
            _logger = logger.CreateLogger<IMessageReceiveBuffer>();

            if (messageBackoffStrategy != null)
            {
                _requestMessageAttributeNames.Add(MessageSystemAttributeName.ApproximateReceiveCount);
            }
        }

        /// <summary>
        /// Starts the receive buffer until it's cancelled by the stopping token.
        /// </summary>
        /// <param name="stoppingToken">A CancellationToken token that will stop the buffer when fired</param>
        /// <returns>A task that throws an `OperationCancelledException` when the cancellation token is fired.</returns>
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
                _logger.LogInformation("Receive buffer for queue {QueueName} has completed, shutting down channel",
                    _sqsQueue.Uri);
                writer.Complete();
                stoppingToken.ThrowIfCancellationRequested();
            }
        }

        private async Task<IList<Message>> GetMessagesAsync(int count, CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            using var receiveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(300));
            IList<Message> messages;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using var linkedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, receiveTimeout.Token);

                var context = new GetMessagesContext
                {
                    Count = count,
                    QueueName = _sqsQueue.QueueName,
                    RegionName = _sqsQueue.RegionSystemName,
                };

                messages = await _sqsMiddleware.RunAsync(context, async () =>
                    await _sqsQueue
                        .GetMessagesAsync(count, _requestMessageAttributeNames, stoppingToken)
                        .ConfigureAwait(false))
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
                using var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                // we don't want to pass the stoppingToken here because
                // we want to process any messages queued messages before stopping
                using var linkedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, stoppingToken);

                try
                {
                    return await writer.WaitToWriteAsync(linkedCts.Token);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // no space in channel, check again
                    continue;
                }
            }

            return false;
        }
    }
}
