using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageHandling
{
    internal sealed class HandlerAdapter<T> : IHandlerWithMessageContext<T>
    {
        private readonly IHandlerAsync<T> _inner;

        public HandlerAdapter(IHandlerAsync<T> inner)
        {
            _inner = inner;
        }

        public async Task<bool> HandleAsync(T message, MessageContext context)
        {
            return await _inner.Handle(message)
                .ConfigureAwait(false);
        }
    }
}
