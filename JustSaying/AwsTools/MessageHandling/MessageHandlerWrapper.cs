using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;

namespace JustSaying.AwsTools.MessageHandling
{
    public class MessageHandlerWrapper
    {
        private readonly IMessageMonitor _messagingMonitor;
        private readonly IMessageLock _messageLock;

        public MessageHandlerWrapper(IMessageLock messageLock, IMessageMonitor messagingMonitor)
        {
            _messageLock = messageLock;
            _messagingMonitor = messagingMonitor;
        }

        public Func<Message, Task<bool>> WrapMessageHandler<T>(IHandlerAsync<T> handler) where T : Message
        {
            handler = MaybeWrapWithGuaranteedDelivery(handler);
            handler = MaybeWrapStopwatch(handler);

            return async message => await handler.Handle((T)message).ConfigureAwait(false);
        }

        private IHandlerAsync<T> MaybeWrapWithGuaranteedDelivery<T>(IHandlerAsync<T> handler) where T : Message
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

            var handlerName = handlerType.FullName.ToLower();
            return new ExactlyOnceHandler<T>(handler, _messageLock, exactlyOnceMetadata.GetTimeOut(), handlerName);
        }

        private IHandlerAsync<T> MaybeWrapStopwatch<T>(IHandlerAsync<T> handler) where T : Message
        {
            var executionTimeMonitoring = _messagingMonitor as IMeasureHandlerExecutionTime;
            if (executionTimeMonitoring == null)
            {
                return handler;
            }

            return new StopwatchHandler<T>(handler, executionTimeMonitoring);
        }
    }
}