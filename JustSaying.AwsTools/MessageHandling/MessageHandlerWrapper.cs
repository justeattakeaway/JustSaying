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

        public Func<Message, Task<bool>> WrapMessageHandler<T>(Func<IAsyncHandler<T>> futureHandler) where T : Message
        {
            IAsyncHandler<T> handler = new FutureHandler<T>(futureHandler);
            handler = MaybeWrapWithGuaranteedDelivery(futureHandler, handler);
            handler = MaybeWrapStopwatch(handler);

            return async message => await handler.Handle((T)message);
        }

        private IAsyncHandler<T> MaybeWrapWithGuaranteedDelivery<T>(Func<IAsyncHandler<T>> futureHandler, IAsyncHandler<T> handler) where T : Message
        {
            var handlerInstance = futureHandler();

            var guaranteedDelivery = new GuaranteedOnceDelivery<T>(handlerInstance);
            if (!guaranteedDelivery.Enabled)
            {
                return handler;
            }

            if (_messageLock == null)
            {
                throw new Exception("IMessageLock is null. You need to specify an implementation for IMessageLock.");
            }

            var handlerName = handlerInstance.GetType().FullName.ToLower();
            return new ExactlyOnceHandler<T>(handler, _messageLock, guaranteedDelivery.TimeOut, handlerName);
        }

        private IAsyncHandler<T> MaybeWrapStopwatch<T>(IAsyncHandler<T> handler) where T : Message
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