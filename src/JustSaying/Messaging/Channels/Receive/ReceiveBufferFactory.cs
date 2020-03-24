using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Configuration;
using JustSaying.Messaging.Channels.ConsumerGroups;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Receive
{
    internal class ReceiveBufferFactory : IReceiveBufferFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConsumerConfig _consumerConfig;
        private readonly IMessageMonitor _monitor;

        public ReceiveBufferFactory(
            ILoggerFactory loggerFactory,
            ConsumerConfig consumerConfig,
            IMessageMonitor monitor)
        {
            _loggerFactory = loggerFactory;
            _consumerConfig = consumerConfig;
            _monitor = monitor;
        }

        public IMessageReceiveBuffer CreateBuffer(ISqsQueue queue, ConsumerGroupSettings consumerGroupSettings)
        {
            var buffer = new MessageReceiveBuffer(
                consumerGroupSettings.BufferSize,
                queue,
                _consumerConfig.SqsMiddleware,
                _monitor,
                _loggerFactory.CreateLogger<MessageReceiveBuffer>());

            return buffer;
        }
    }
}
