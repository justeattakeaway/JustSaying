using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    internal sealed class MessageHandlerWrapper
    {
        private readonly IMessageMonitor _messagingMonitor;
        private readonly IMessageLockAsync _messageLock;
        private readonly ILoggerFactory _loggerFactory;

        public MessageHandlerWrapper(
            IMessageLockAsync messageLock,
            IMessageMonitor messagingMonitor,
            ILoggerFactory loggerFactory)
        {
            _messageLock = messageLock;
            _messagingMonitor = messagingMonitor;
            _loggerFactory = loggerFactory;
        }

        public Func<Message, Task<bool>> WrapMessageHandler<T>(Func<IHandlerAsync<T>> futureHandler) where T : Message
        {
            return async message =>
            {
                var handler = futureHandler();
                handler = MaybeWrapWithExactlyOnce(handler);
                handler = MaybeWrapWithStopwatch(handler);

                return await handler.Handle((T)message).ConfigureAwait(false);
            };
        }

        private IHandlerAsync<T> MaybeWrapWithExactlyOnce<T>(IHandlerAsync<T> handler) where T : Message
        {
            var handlerType = handler.GetType();
            var exactlyOnceMetadata = new ExactlyOnceReader(handlerType);
            if (!exactlyOnceMetadata.Enabled)
            {
                return handler;
            }

            if (_messageLock == null)
            {
                throw new Exception("IMessageLock is null. You need to specify an implementation for IMessageLock.");
            }

            var handlerName = handlerType.FullName.ToLowerInvariant();
            var timeout = TimeSpan.FromSeconds(exactlyOnceMetadata.GetTimeOut());
            var logger = _loggerFactory.CreateLogger<ExactlyOnceHandler<T>>();

            return new ExactlyOnceHandler<T>(handler, _messageLock, timeout, handlerName, logger);
        }

        private IHandlerAsync<T> MaybeWrapWithStopwatch<T>(IHandlerAsync<T> handler) where T : Message
        {
            if (!(_messagingMonitor is IMeasureHandlerExecutionTime executionTimeMonitoring))
            {
                return handler;
            }

            return new StopwatchHandler<T>(handler, executionTimeMonitoring);
        }
    }
}
