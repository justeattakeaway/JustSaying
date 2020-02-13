using System.Threading;

namespace JustSaying.Messaging.Channels
{
    public static partial class ConsumerBus
    {
        public static void Start(CancellationToken stoppingToken)
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

            consumer1.ConsumeFrom(multiplexer.Messages());
            consumer2.ConsumeFrom(multiplexer.Messages());

            consumer1.Start();
            consumer2.Start();
            multiplexer.Start();

            buffer1.Start(stoppingToken);
            buffer2.Start(stoppingToken);
        }
    }
}
