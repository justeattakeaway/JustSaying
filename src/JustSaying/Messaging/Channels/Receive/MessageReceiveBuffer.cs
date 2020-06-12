using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Receive
{
    internal class MessageReceiveBuffer : IMessageReceiveBuffer
    {
        private readonly Channel<IQueueMessageContext> _channel;
        private readonly int _prefetch;
        private readonly int _bufferSize;
        private readonly TimeSpan _readTimeout;
        private readonly ISqsQueue _sqsQueue;
        private readonly MiddlewareBase<GetMessagesContext, IList<Message>> _sqsMiddleware;
        private readonly IMessageMonitor _monitor;
        private readonly ILogger _logger;

        private readonly List<string> _requestMessageAttributeNames = new List<string>();
        private string _backoffStrategyName;

        public ChannelReader<IQueueMessageContext> Reader => _channel.Reader;

        public MessageReceiveBuffer(
            int prefetch,
            int bufferSize,
            TimeSpan readTimeout,
            ISqsQueue sqsQueue,
            MiddlewareBase<GetMessagesContext, IList<Message>> sqsMiddleware,
            IMessageMonitor monitor,
            ILogger<IMessageReceiveBuffer> logger,
            IMessageBackoffStrategy messageBackoffStrategy = null)
        {
            _channel = Channel.CreateBounded<IQueueMessageContext>(bufferSize);
            _prefetch = prefetch;
            _bufferSize = bufferSize;
            _readTimeout = readTimeout;
            _sqsQueue = sqsQueue ?? throw new ArgumentNullException(nameof(sqsQueue));
            _sqsMiddleware = sqsMiddleware ?? throw new ArgumentNullException(nameof(sqsMiddleware));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backoffStrategyName = messageBackoffStrategy?.GetType()?.Name;

            if (messageBackoffStrategy != null)
            {
                _requestMessageAttributeNames.Add(MessageSystemAttributeName.ApproximateReceiveCount);
            }
        }

        /// <summary>
        /// Starts the receive buffer until it's cancelled by the stopping token.
        /// </summary>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> that will stop the buffer when signalled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation to receive the messages.</returns>
        public async Task RunAsync(CancellationToken stoppingToken)
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
                        bool canWrite = await writer.WaitToWriteAsync(stoppingToken).ConfigureAwait(false);
                        if (!canWrite) break;
                    }

                    IList<Message> messages;
                    using (_monitor.MeasureReceive(_sqsQueue.QueueName, _sqsQueue.RegionSystemName))
                    {
                        messages = await GetMessagesAsync(_bufferSize, stoppingToken).ConfigureAwait(false);

                        if (messages == null) continue;
                    }

                    foreach (Message message in messages)
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
            }
        }

        private async Task<IList<Message>> GetMessagesAsync(int count, CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            using var receiveTimeout = new CancellationTokenSource(_readTimeout);
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

                _requestMessageAttributeNames.Add("content");

                messages = await _sqsMiddleware.RunAsync(context,
                        async ct =>
                            await _sqsQueue
                                .GetMessagesAsync(count, _requestMessageAttributeNames, ct)
                                .ConfigureAwait(false),
                        linkedCts.Token)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (receiveTimeout.Token.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "Timed out while receiving messages from queue '{QueueName}' in region '{Region}'.",
                        _sqsQueue.QueueName,
                        _sqsQueue.RegionSystemName);
                }
            }

            stopwatch.Stop();

            _monitor.ReceiveMessageTime(stopwatch.Elapsed, _sqsQueue.QueueName, _sqsQueue.RegionSystemName);

            return messages;
        }

        public object Interrogate()
        {
            return new
            {
                BufferSize = _bufferSize,
                _sqsQueue.QueueName,
                Region = _sqsQueue.RegionSystemName,
                Prefetch = _prefetch,
                BackoffStrategyName = _backoffStrategyName,
            };
        }
    }
}
