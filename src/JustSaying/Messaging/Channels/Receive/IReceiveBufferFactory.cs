using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Configuration;
using JustSaying.Messaging.Channels.ConsumerGroups;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Receive
{
    internal interface IReceiveBufferFactory
    {
        IMessageReceiveBuffer CreateBuffer(ISqsQueue queue, ConsumerGroupSettings consumerGroupSettings);
    }

    internal class ReceiveBufferFactory : IReceiveBufferFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConsumerGroupConfig _consumerGroupConfig;
        private readonly IMessageMonitor _monitor;

        public ReceiveBufferFactory(
            ILoggerFactory loggerFactory,
            ConsumerGroupConfig consumerGroupConfig,
            IMessageMonitor monitor)
        {
            _loggerFactory = loggerFactory;
            _consumerGroupConfig = consumerGroupConfig;
            _monitor = monitor;
        }

        public IMessageReceiveBuffer CreateBuffer(ISqsQueue queue, ConsumerGroupSettings consumerGroupSettings)
        {
            var buffer = new MessageReceiveBuffer(
                consumerGroupSettings.BufferSize,
                queue,
                _consumerGroupConfig.SqsMiddleware,
                _monitor,
                _loggerFactory.CreateLogger<MessageReceiveBuffer>());

            return buffer;
        }
    }
}
