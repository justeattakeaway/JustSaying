using System;
using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class FutureHandler<T> : IAsyncHandler<T> where T : Message
    {
        private readonly Func<IAsyncHandler<T>> _futureHandler;

        public FutureHandler(Func<IAsyncHandler<T>> futureHandler)
        {
            _futureHandler = futureHandler;
        }

        public async Task<bool> Handle(T message)
        {
            var handler = _futureHandler();
            return await handler.Handle(message);
        }
    }
}