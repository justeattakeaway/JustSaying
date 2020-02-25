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
    internal interface IDownloadBuffer
    {
        Task Start(CancellationToken stoppingToken);
        ChannelReader<IQueueMessageContext> Reader { get; }
    }

    internal class DownloadBuffer : IDownloadBuffer
    {
        private readonly Channel<IQueueMessageContext> _channel;
        private readonly int _bufferLength;
        private readonly ISqsQueue _sqsQueue;
        private readonly ILogger _logger;

        // todo: add logic around populating this
        private readonly List<string> _requestMessageAttributeNames = new List<string>();

        public ChannelReader<IQueueMessageContext> Reader => _channel.Reader;

        public DownloadBuffer(
            int bufferLength,
            ISqsQueue sqsQueue,
            ILoggerFactory logger)
        {
            _bufferLength = bufferLength;
            _sqsQueue = sqsQueue;
            _logger = logger.CreateLogger<IDownloadBuffer>();
            _channel = Channel.CreateBounded<IQueueMessageContext>(bufferLength);
        }

        public async Task Start(CancellationToken stoppingToken)
        {
            await Task.Yield();

            ChannelWriter<IQueueMessageContext> writer = _channel.Writer;
            CancellationTokenSource linkedCts = null;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // we don't want to pass the stoppingToken here because
                    // we want to process any messages queued messages before stopping
                    using var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, stoppingToken);

                    using (_logger.TimedOperation("Downloader waiting for buffer to empty before writing"))
                    {
                        bool writePermitted = await writer.WaitToWriteAsync(linkedCts.Token);
                        if (!writePermitted)
                        {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // no space in channel, break to check if stoppingToken is cancelled
                    continue;
                }
                finally
                {
                    linkedCts?.Dispose();
                }

                if (stoppingToken.IsCancellationRequested) break;

                IList<Message> messages;
                using (_logger.TimedOperation("Receiving messages from SQS for queue {QueueName}", _sqsQueue.QueueName))
                {
                    messages = await _sqsQueue
                        .GetMessages(_bufferLength, _requestMessageAttributeNames, stoppingToken)
                        .ConfigureAwait(false);
                    if (messages == null) continue;
                }

                foreach (var message in messages)
                {
                    IQueueMessageContext messageContext = _sqsQueue.CreateQueueMessageContext(message);
                    await writer.WriteAsync(messageContext).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("Downloader for queue {QueueName} has completed, shutting down channel...", _sqsQueue.Uri);

            writer.Complete();

        }
    }
}
