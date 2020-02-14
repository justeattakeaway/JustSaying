using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JustSaying.Messaging.Channels
{
    internal class ConsumerBus
    {
        private readonly IMultiplexer _multiplexer;
        private IList<IDownloadBuffer> _downloadBuffers;

        public ConsumerBus(ILoggerFactory loggerFactory)
        {
            _multiplexer = new RoundRobinQueueMultiplexer(loggerFactory);
            _downloadBuffers = new List<IDownloadBuffer>();
        }

        public void AddDownloadBuffer(IDownloadBuffer downloadBuffer)
        {
            _downloadBuffers.Add(downloadBuffer);
        }

        public void Start(CancellationToken stoppingToken)
        {
            // creates and owns core channel

            // create download buffers (one-per-queue)
            // create n consumers (defined by config)

            // link download buffers to core channel
            // link consumers to core channel

            // start()

            var loggerFactory = new NullLoggerFactory();

            IDownloadBuffer buffer1 = null;
            IDownloadBuffer buffer2 = null;
            IChannelConsumer consumer1 = null;
            IChannelConsumer consumer2 = null;

            _multiplexer.ReadFrom(buffer1.Reader);
            _multiplexer.ReadFrom(buffer2.Reader);

            consumer1.ConsumeFrom(_multiplexer.Messages());
            consumer2.ConsumeFrom(_multiplexer.Messages());

            consumer1.Start();
            consumer2.Start();
            multiplexer.Start();

            buffer1.Start(stoppingToken);
            buffer2.Start(stoppingToken);
        }
    }
}
