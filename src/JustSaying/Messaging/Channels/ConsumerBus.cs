using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

namespace JustSaying.Messaging.Channels
{
    public class ConsumerBus
    {
        public void Start(CancellationToken stoppingToken)
        {
            // creates and owns core channel

            // create download buffers (one-per-queue)
            // create n consumers (defined by config)

            // link download buffers to core channel
            // link consumers to core channel

            // start()

            IDownloadBuffer buffer1 = null;
            IDownloadBuffer buffer2 = null;
            IMultiplexer multiplexer = null;
            IChannelConsumer consumer1 = null;
            IChannelConsumer consumer2 = null;

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);

            consumer1.ConsumeFrom(multiplexer);
            consumer2.ConsumeFrom(multiplexer);

            var cts = new CancellationTokenSource();

            consumer1.Start();
            consumer2.Start();
            multiplexer.Start();

            buffer1.Start(cts.Token);
            buffer2.Start(cts.Token);

        }

        internal interface IMultiplexer
        {
            void Start();
            void ReadFrom(ChannelReader<QueueMessageContext> reader);
            IAsyncEnumerable<QueueMessageContext> Messages();
        }

        internal interface IDownloadBuffer
        {
            void Start(CancellationToken stoppingToken);
            ChannelReader<QueueMessageContext> Reader { get; }
        }

        internal interface IChannelConsumer
        {
            void Start();
            void ConsumeFrom(IMultiplexer multiplexer);
        }
    }
}
