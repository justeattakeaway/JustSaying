using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class FutureHandler<T> : IHandlerAsync<T> where T : Message
    {
        private readonly MessageHandlerWrapper _messageHandlerWrapper;
        public IHandlerAndMetadataResolver Resolver { get; set; }
        public HandlerResolutionContext Context { get; set; }

        public FutureHandler(IHandlerAndMetadataResolver handlerResolver, HandlerResolutionContext context, MessageHandlerWrapper messageHandlerWrapper)
        {
            Resolver = handlerResolver;
            Context = context;
            _messageHandlerWrapper = messageHandlerWrapper;
        }
        
        public async Task<bool> Handle(T message)
        {
            var handler = Resolver.ResolveHandler<T>(Context.WithMessage(message));
            var handlerFunc = _messageHandlerWrapper.WrapMessageHandler(handler);

            return await handlerFunc(message).ConfigureAwait(false);
        }
    }
}