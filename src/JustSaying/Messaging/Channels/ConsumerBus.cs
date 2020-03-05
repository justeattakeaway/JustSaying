using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Monitoring;
using JustSaying.Messaging.Policies;
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
            SqsPolicyAsync<IList<Amazon.SQS.Model.Message>> sqsPolicy,
            IMessageDispatcher messageDispatcher,
            IMessageMonitor monitor,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ConsumerBus>();
            _multiplexer = new RoundRobinQueueMultiplexer(loggerFactory);

            _buffers = queues
                .Select(q => CreateBuffer(q, sqsPolicy, monitor, loggerFactory))
                .ToList();

            // create n consumers (defined by config)
            // link consumers to core channel
            _consumers = Enumerable.Range(0, numberOfConsumers)
                .Select(x => new ChannelConsumer(messageDispatcher)
                    .ConsumeFrom(_multiplexer.Messages()))
                .ToList();
        }

        public Task Run(CancellationToken stoppingToken)
        {
            var numberOfConsumers = _consumers.Count;
            _logger.LogInformation(
                "Starting up consumer bus with {ConsumerCount} consumers and {DownloadBufferCount} downloaders",
                numberOfConsumers, _buffers.Count);

            // start
            var completionTasks = new List<Task>();

            completionTasks.Add( _multiplexer.Run(stoppingToken));
            completionTasks.AddRange(_consumers.Select(x => x.Run(stoppingToken)));
            completionTasks.AddRange(_buffers.Select(x => x.Run(stoppingToken)));

            _logger.LogInformation("Consumer bus successfully started");

            return Task.WhenAll(completionTasks);
        }

        private IMessageReceiveBuffer CreateBuffer(
            ISqsQueue queue,
            SqsPolicyAsync<IList<Amazon.SQS.Model.Message>> sqsPolicy,
            IMessageMonitor monitor,
            ILoggerFactory loggerFactory)
        {
            int bufferLength = 10;
            var buffer = new MessageReceiveBuffer(
                bufferLength,
                queue,
                sqsPolicy,
                monitor,
                loggerFactory);

            // link download buffers to core channel
            _multiplexer.ReadFrom(buffer.Reader);

            return buffer;
        }
    }
}
