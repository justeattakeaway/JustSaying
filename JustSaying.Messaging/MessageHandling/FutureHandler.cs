using System;
using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class FutureHandler<T> : IHandlerAsync<T> where T : Message
    {
        private readonly Func<IHandlerAsync<T>> _futureHandler;

        public FutureHandler(Func<IHandlerAsync<T>> futureHandler)
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