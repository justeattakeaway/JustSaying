using System;
using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class ExactlyOnceHandler<T> : IHandlerAsync<T> where T : Message
    {
        private readonly IHandlerAsync<T> _inner;
        private readonly IMessageLock _messageLock;
        private readonly int _timeOut;
        private readonly string _handlerName;

        public ExactlyOnceHandler(IHandlerAsync<T> inner, IMessageLock messageLock, int timeOut, string handlerName)
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
            var lockKey = string.Format("{2}-{1}-{0}", _handlerName, typeof(T).Name.ToLower(), message.UniqueKey());
            var lockResponse = _messageLock.TryAquireLock(lockKey, TimeSpan.FromSeconds(_timeOut));
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
                var successfullyHandled = await _inner.Handle(message);
                if (successfullyHandled)
                {
                    _messageLock.TryAquireLockPermanently(lockKey);
                }
                return successfullyHandled;
            }
            catch
            {
                _messageLock.ReleaseLock(lockKey);
                throw;
            }
        }
    }
}
