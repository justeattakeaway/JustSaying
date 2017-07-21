using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class FutureHandler<T> : IHandlerAsync<T> where T : Message
    {
        private readonly IHandlerAsync<T> _handler;

        public FutureHandler(IHandlerAndMetadataResolver handlerResolver, HandlerResolutionContext context)
        {
            Resolver = handlerResolver;
            Context = context;
        }

        public FutureHandler(IHandlerAsync<T> handler, HandlerResolutionContext context)
        {
            _handler = handler;
            Context = context;
        }

        public IHandlerAndMetadataResolver Resolver { get; set; }
        public HandlerResolutionContext Context { get; set; }

        //TODO unify handler & resolver
        public async Task<bool> Handle(T message)
            => await (_handler ?? Resolver.ResolveHandler<T>(Context.WithMessage(message))).Handle(message).ConfigureAwait(false);
    }
}