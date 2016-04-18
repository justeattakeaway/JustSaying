using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageHandling
{
    public class AsyncingHandler<T> : IAsyncHandler<T>
    {
        private readonly IHandler<T> _syncHandler;

        public AsyncingHandler(IHandler<T> syncHandler)
        {
            _syncHandler = syncHandler;
        }

        public Task<bool> Handle(T message)
        {
            return Task.FromResult(_syncHandler.Handle(message));
        }
    }
}
