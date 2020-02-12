using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels
{
    internal interface IDownloadBuffer
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

        public ChannelReader<IQueueMessageContext> Reader { get; }

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
                bool writePermitted = await writer.WaitToWriteAsync(stoppingToken);
                if (!writePermitted)
                {
                    break;
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
