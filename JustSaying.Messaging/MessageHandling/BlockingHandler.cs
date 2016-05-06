using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageHandling
{

    /// <summary>
    /// Used to convert "IHandler " instances into IAsyncHandler
    /// So that the rest of the system only has to deal with IAsyncHandler
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BlockingHandler<T> : IHandlerAsync<T>
    {
        private readonly IHandler<T> _inner;

        public BlockingHandler(IHandler<T> inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException("inner");
            }

            _inner = inner;
        }

        public IHandler<T> Inner { get { return _inner; } }

        public Task<bool> Handle(T message)
        {
            return Task.FromResult(_inner.Handle(message));
        }
    }
}
