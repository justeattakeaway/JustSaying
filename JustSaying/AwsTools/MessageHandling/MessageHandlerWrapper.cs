using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;

namespace JustSaying.AwsTools.MessageHandling
{
    public class MessageHandlerWrapper
    {
        private readonly IMessageMonitor _messagingMonitor;
        private readonly IMessageLockAsync _messageLock;

        public MessageHandlerWrapper(IMessageLockAsync messageLock, IMessageMonitor messagingMonitor)
        {
            _messageLock = messageLock;
            _messagingMonitor = messagingMonitor;
        }

        public Func<Message, CancellationToken, Task<bool>> WrapMessageHandler<T>(Func<ICancellableHandlerAsync<T>> futureHandler) where T : Message
        {
            return async (message, cancellationToken) =>
            {
                ICancellableHandlerAsync<T> rootHandler = futureHandler();
                ICancellableHandlerAsync<T> handler = rootHandler;

                handler = MaybeWrapWithExactlyOnce(handler, rootHandler.GetType());
                handler = MaybeWrapWithStopwatch(handler);

                return await handler.HandleAsync((T)message, cancellationToken).ConfigureAwait(false);
            };
        }

        public Func<Message, CancellationToken, Task<bool>> WrapMessageHandler<T>(Func<IHandlerAsync<T>> futureHandler) where T : Message
        {
            return async (message, cancellationToken) =>
            {
                IHandlerAsync<T> rootHandler = futureHandler();
                ICancellableHandlerAsync<T> handler = new CancellableHandlerAdapter<T>(rootHandler);

                handler = MaybeWrapWithExactlyOnce(handler, rootHandler.GetType());
                handler = MaybeWrapWithStopwatch(handler);

                return await handler.HandleAsync((T)message, CancellationToken.None).ConfigureAwait(false);
            };
        }

        private ICancellableHandlerAsync<T> MaybeWrapWithExactlyOnce<T>(ICancellableHandlerAsync<T> handler, Type handlerType) where T : Message
        {
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
            return new ExactlyOnceHandler<T>(handler, _messageLock, exactlyOnceMetadata.GetTimeOut(), handlerName);
        }

        private ICancellableHandlerAsync<T> MaybeWrapWithStopwatch<T>(ICancellableHandlerAsync<T> handler) where T : Message
        {
            if (!(_messagingMonitor is IMeasureHandlerExecutionTime executionTimeMonitoring))
            {
                return handler;
            }

            return new StopwatchHandler<T>(handler, executionTimeMonitoring);
        }

        private sealed class CancellableHandlerAdapter<T> : ICancellableHandlerAsync<T>
        {
            private readonly IHandlerAsync<T> _inner;

            internal CancellableHandlerAdapter(IHandlerAsync<T> inner)
            {
                _inner = inner;
            }

            public async Task<bool> Handle(T message)
            {
                return await _inner.Handle(message).ConfigureAwait(false);
            }

            public async Task<bool> HandleAsync(T message, CancellationToken cancellationToken)
            {
                return await _inner.Handle(message).ConfigureAwait(false);
            }
        }
    }
}
