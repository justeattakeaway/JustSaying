using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class FutureHandler<T> : IHandlerAsync<T> where T : Message
    {
        public IHandlerAndMetadataResolver Resolver { get; set; }
        public HandlerResolutionContext Context { get; set; }

        public FutureHandler(IHandlerAndMetadataResolver handlerResolver, HandlerResolutionContext context)
        {
            Resolver = handlerResolver;
            Context = context;
        }

        public FutureHandler(IHandlerAsync<T> handler, HandlerResolutionContext context)
        {
            Resolver = new PredefinedHandlerResolver<T>(handler);
            Context = context;
        }

        public async Task<bool> Handle(T message)
        {
            var handler = Resolver.ResolveHandler<T>(Context.WithMessage(message));
            return await handler.Handle(message).ConfigureAwait(false);
        }
    }
}