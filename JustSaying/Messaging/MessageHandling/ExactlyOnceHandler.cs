using System;
using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class ExactlyOnceHandler<T> : IHandlerAsync<T> where T : Message
    {
        private readonly IHandlerAsync<T> _inner;
        private readonly IMessageLockAsync _messageLock;
        private readonly int _timeOut;
        private readonly string _handlerName;

        public ExactlyOnceHandler(IHandlerAsync<T> inner, IMessageLockAsync messageLock, int timeOut, string handlerName)
        {
            _inner = inner;
            _messageLock = messageLock;
            _timeOut = timeOut;
            _handlerName = handlerName;
        }

        private const bool RemoveTheMessageFromTheQueue = true;
        private const bool LeaveItInTheQueue = false;
        
        public async Task<bool> Handle(T message)
        {
            var lockKey = $"{message.UniqueKey()}-{typeof(T).Name.ToLower()}-{_handlerName}";
            var lockResponse = await _messageLock.TryAquireLockAsync(lockKey, TimeSpan.FromSeconds(_timeOut));
            if (!lockResponse.DoIHaveExclusiveLock)
            {
                if (lockResponse.IsMessagePermanentlyLocked)
                {
                    return RemoveTheMessageFromTheQueue;
                }

                return LeaveItInTheQueue;
            }

            try
            {
                var successfullyHandled = await _inner.Handle(message).ConfigureAwait(false);
                if (successfullyHandled)
                {
                    await _messageLock.TryAquireLockPermanentlyAsync(lockKey);
                }
                return successfullyHandled;
            }
            catch
            {
                await _messageLock.ReleaseLockAsync(lockKey);
                throw;
            }
        }
    }
}
