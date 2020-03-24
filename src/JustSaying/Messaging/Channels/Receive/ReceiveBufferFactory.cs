using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Configuration;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Receive
{
    internal class ReceiveBufferFactory : IReceiveBufferFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly SubscriptionConfig _subscriptionConfig;
        private readonly IMessageMonitor _monitor;

        public ReceiveBufferFactory(
            ILoggerFactory loggerFactory,
            SubscriptionConfig subscriptionConfig,
            IMessageMonitor monitor)
        {
            _loggerFactory = loggerFactory;
            _subscriptionConfig = subscriptionConfig;
            _monitor = monitor;
        }

        public IMessageReceiveBuffer CreateBuffer(ISqsQueue queue, SubscriptionGroupSettings subscriptionGroupSettings)
        {
            var buffer = new MessageReceiveBuffer(
                subscriptionGroupSettings.BufferSize,
                queue,
                _subscriptionConfig.SqsMiddleware,
                _monitor,
                _loggerFactory.CreateLogger<MessageReceiveBuffer>());

            return buffer;
        }
    }
}
