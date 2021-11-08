using JustSaying.Messaging.Middleware;
using JustSaying.Models;

namespace JustSaying.TestingFramework
{
    public class InspectableMiddleware<TMessage> : MiddlewareBase<HandleMessageContext, bool> where TMessage : Message
    {
        public InspectableMiddleware()
        {
            Handler = new InspectableHandler<TMessage>();
        }

        public InspectableHandler<TMessage> Handler { get; }

        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            await Handler.Handle(context.MessageAs<TMessage>()).ConfigureAwait(false);
            return true;
        }
    }
}
