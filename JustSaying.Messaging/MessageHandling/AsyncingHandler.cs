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

#pragma warning restore CS0618 // Type or member is obsolete
}
