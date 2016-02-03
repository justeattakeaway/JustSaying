using System;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class FutureHandler<T> : IHandler<T> where T : Message
    {
        private readonly Func<IHandler<T>> _futureHandler;

        public FutureHandler(Func<IHandler<T>> futureHandler)
        {
            _futureHandler = futureHandler;
        }

        public bool Handle(T message)
        {
            return _futureHandler().Handle(message);
        }
    }
}