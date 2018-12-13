using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageHandling
{
    public interface IHandlerWithContext<in T>
    {
        Task<bool> Handle(T message, MessageContext context);
    }

    public class HandlerAdapter<T> : IHandlerWithContext<T>
    {
        private readonly IHandlerAsync<T> _inner;

        public HandlerAdapter(IHandlerAsync<T> inner)
        {
            _inner = inner;
        }

        public Task<bool> Handle(T message, MessageContext context)
        {
            return _inner.Handle(message);
        }
    }

}
