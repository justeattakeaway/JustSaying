using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling.Dispatch
{
    internal sealed class MessageHandlerWrapper
    {
        private readonly IMessageMonitor _messagingMonitor;
        private readonly ILoggerFactory _loggerFactory;

        public MessageHandlerWrapper(
            IMessageMonitor messagingMonitor,
            ILoggerFactory loggerFactory)
        {
            _messagingMonitor = messagingMonitor;
            _loggerFactory = loggerFactory;
        }

        public Func<Message, Task<bool>> WrapMessageHandler<T>(Func<IHandlerAsync<T>> futureHandler) where T : Message
        {
            return async message =>
            {
                var handler = futureHandler();
                handler = MaybeWrapWithStopwatch(handler);

                return await handler.Handle((T)message).ConfigureAwait(false);
            };
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
