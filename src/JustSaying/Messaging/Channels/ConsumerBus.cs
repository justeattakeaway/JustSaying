using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels
{
    public interface IConsumerBus
    {
        void Start(CancellationToken stoppingToken);
        Task Completion { get; }
    }

    internal class ConsumerBus : IConsumerBus
    {
        private readonly IMultiplexer _multiplexer;
        private readonly IList<IDownloadBuffer> _downloadBuffers;
        private readonly IList<IChannelConsumer> _consumers;
        private readonly ILogger _logger;

        internal ConsumerBus(
            IList<ISqsQueue> queues,
            int numberOfConsumers,
            IMessageDispatcher messageDispatcher,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ConsumerBus>();
            _multiplexer = new RoundRobinQueueMultiplexer(loggerFactory);

            _downloadBuffers = queues
                .Select(q => CreateDownloadBuffer(q, loggerFactory))
                .ToList();

            // create n consumers (defined by config)
            // link consumers to core channel
            _consumers = Enumerable.Range(0, numberOfConsumers)
                .Select(x => new ChannelConsumer(messageDispatcher, loggerFactory)
                    .ConsumeFrom(_multiplexer.Messages()))
                .ToList();
        }

        public void Start(CancellationToken stoppingToken)
        {
            var numberOfConsumers = _consumers.Count;
            _logger.LogInformation("Starting up consumer bus with {ConsumerCount} consumers and {DownloadBufferCount} downloaders",
                 numberOfConsumers, _downloadBuffers.Count);

            // start
            var startTasks = new List<Task>();
            startTasks.Add(_multiplexer.Start());

            startTasks.AddRange(_consumers.Select(x => x.Start()));
            startTasks.AddRange(_downloadBuffers.Select(x => x.Start(stoppingToken)));

            _logger.LogInformation("Consumer bus successfully started");

            Completion = Task.WhenAll(startTasks);
        }

        public Task Completion { get; private set; }

        private IDownloadBuffer CreateDownloadBuffer(ISqsQueue queue, ILoggerFactory loggerFactory)
        {
            int bufferLength = 10;
            var buffer = new DownloadBuffer(bufferLength, queue, loggerFactory);

            // link download buffers to core channel
            _multiplexer.ReadFrom(buffer.Reader);

            return buffer;
        }
    }
}
