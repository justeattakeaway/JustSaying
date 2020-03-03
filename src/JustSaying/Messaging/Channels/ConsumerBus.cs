using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels
{
    internal class ConsumerBus : IConsumerBus
    {
        private readonly IMultiplexer _multiplexer;
        private readonly IList<IMessageReceiveBuffer> _buffers;
        private readonly IList<IChannelConsumer> _consumers;
        private readonly ILogger _logger;

        internal ConsumerBus(
            IList<ISqsQueue> queues,
            int numberOfConsumers,
            IMessageDispatcher messageDispatcher,
            IMessageMonitor monitor,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ConsumerBus>();
            _multiplexer = new RoundRobinQueueMultiplexer(loggerFactory);

            _buffers = queues
                .Select(q => CreateBuffer(q, monitor, loggerFactory))
                .ToList();

            // create n consumers (defined by config)
            // link consumers to core channel
            _consumers = Enumerable.Range(0, numberOfConsumers)
                .Select(x => new ChannelConsumer(messageDispatcher, loggerFactory)
                    .ConsumeFrom(_multiplexer.Messages()))
                .ToList();
        }

        public Task Start(CancellationToken stoppingToken)
        {
            var numberOfConsumers = _consumers.Count;
            _logger.LogInformation(
                "Starting up consumer bus with {ConsumerCount} consumers and {DownloadBufferCount} downloaders",
                numberOfConsumers, _buffers.Count);

            // start
            var completionTasks = new List<Task>();
            completionTasks.Add(_multiplexer.Start());

            completionTasks.AddRange(_consumers.Select(x => x.Start()));
            completionTasks.AddRange(_buffers.Select(x => x.Start(stoppingToken)));

            _logger.LogInformation("Consumer bus successfully started");

            return Task.WhenAll(completionTasks);
        }

        private IMessageReceiveBuffer CreateBuffer(
            ISqsQueue queue,
            IMessageMonitor monitor,
            ILoggerFactory loggerFactory)
        {
            int bufferLength = 10;
            var buffer = new MessageReceiveBuffer(bufferLength, queue, monitor, loggerFactory);

            // link download buffers to core channel
            _multiplexer.ReadFrom(buffer.Reader);

            return buffer;
        }
    }
}
