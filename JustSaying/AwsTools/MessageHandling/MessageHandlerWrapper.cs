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

        public Func<Message, Task<bool>> WrapMessageHandler<T>(FutureHandler<T> futureHandler) where T : Message
        {
            var handler = MaybeWrapWithGuaranteedDelivery(futureHandler);
            handler = MaybeWrapStopwatch(handler);

            return async message => await handler.Handle((T)message).ConfigureAwait(false);
        }

        private IHandlerAsync<T> MaybeWrapWithGuaranteedDelivery<T>(FutureHandler<T> futureHandler) where T : Message
        {
            var handlerType = futureHandler.Resolver.ResolveHandlerType<T>(futureHandler.Context);//TODO [SP] invert the FutureHandler and Wrapper so that Wrapping occurs just in time

            var exactlyOnceMetadata = new ExactlyOnceReader(handlerType);
            if (!exactlyOnceMetadata.Enabled)
            {
                return futureHandler;
            }

            if (_messageLock == null)
            {
                throw new Exception("IMessageLock is null. You need to specify an implementation for IMessageLock.");
            }

            var handlerName = handlerType.FullName.ToLower();
            return new ExactlyOnceHandler<T>(futureHandler, _messageLock, exactlyOnceMetadata.GetTimeOut(), handlerName);
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