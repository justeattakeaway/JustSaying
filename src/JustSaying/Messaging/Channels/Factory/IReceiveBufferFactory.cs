using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Factory
{
    internal interface IReceiveBufferFactory
    {
        IMessageReceiveBuffer CreateBuffer(ISqsQueue queue);
    }

    class ReceiveBufferFactory : IReceiveBufferFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConsumerConfig _consumerConfig;
        private readonly IMessageMonitor _monitor;

        public ReceiveBufferFactory(
            ILoggerFactory loggerFactory,
            IConsumerConfig consumerConfig,
            IMessageMonitor monitor)
        {
            _loggerFactory = loggerFactory;
            _consumerConfig = consumerConfig;
            _monitor = monitor;
        }

        public IMessageReceiveBuffer CreateBuffer(ISqsQueue queue)
        {
            var buffer = new MessageReceiveBuffer(
                _consumerConfig.BufferSize,
                queue,
                _consumerConfig.SqsMiddleware,
                _monitor,
                _loggerFactory.CreateLogger<MessageReceiveBuffer>());

            return buffer;
        }
    }
}
