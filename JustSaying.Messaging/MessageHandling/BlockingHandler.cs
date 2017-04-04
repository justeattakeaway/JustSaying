using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageHandling
{

    /// <summary>
    /// Used to convert "IHandler " instances into IAsyncHandler
    /// So that the rest of the system only has to deal with IAsyncHandler
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("Use IHandlerAsync")]
    public class BlockingHandler<T> : IHandlerAsync<T>
    {
        public BlockingHandler(IHandler<T> inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            Inner = inner;
        }

        public IHandler<T> Inner { get; }

        public Task<bool> Handle(T message) => Task.FromResult(Inner.Handle(message));
    }
}
