using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageHandling
{
#pragma warning disable CS0618 // Type or member is obsolete

    /// <summary>
    /// Used to convert "IHandler " instances into IAsyncHandler
    /// So that the rest of the system only has to deal with IAsyncHandler
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncingHandler<T> : IHandlerAsync<T>
    {
        private readonly IHandler<T> _inner;

        public AsyncingHandler(IHandler<T> inner)
        {
            _inner = inner;
        }

        public IHandler<T> Inner { get { return _inner; } }

        public Task<bool> Handle(T message)
        {
            return Task.FromResult(_inner.Handle(message));
        }
    }

#pragma warning restore CS0618 // Type or member is obsolete
}
