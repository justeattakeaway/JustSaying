using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels
{
    public interface IDownloadBuffer
    {
        Task Start(CancellationToken stoppingToken);
        ChannelReader<IQueueMessageContext> Reader { get; }
    }

    public class DownloadBuffer : IDownloadBuffer
    {
        private readonly Channel<IQueueMessageContext> _channel;
        private readonly int _bufferLength;
        private readonly ISqsQueue _sqsQueue;

        // todo: add logic around populating this
        private readonly List<string> _requestMessageAttributeNames = new List<string>();

        public ChannelReader<IQueueMessageContext> Reader => _channel.Reader;

        public DownloadBuffer(
            int bufferLength,
            ISqsQueue sqsQueue)
        {
            _bufferLength = bufferLength;
            _sqsQueue = sqsQueue;
            _channel = Channel.CreateBounded<IQueueMessageContext>(bufferLength);
        }

        public async Task Start(CancellationToken stoppingToken)
        {
            ChannelWriter<IQueueMessageContext> writer = _channel.Writer;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // we don't want to pass the stoppingToken here because
                    // we want to process any messages queued messages before stopping
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(2));

                    bool writePermitted = await writer.WaitToWriteAsync(cts.Token);
                    if (!writePermitted)
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // no space in channel, break to check if stoppingToken is cancelled
                    continue;
                }

                var messages = await _sqsQueue.GetMessages(_bufferLength, _requestMessageAttributeNames, stoppingToken).ConfigureAwait(false);
                if (messages == null) continue;

                foreach (var message in messages)
                {
                    IQueueMessageContext messageContext = _sqsQueue.CreateQueueMessageContext(message);
                    await writer.WriteAsync(messageContext).ConfigureAwait(false);
                }
            }

            writer.Complete();
        }
    }
}
