using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.Middleware.MessageContext
{
    public class MessageContextAccessorMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly IMessageContextAccessor _messageContextAccessor;

        public MessageContextAccessorMiddleware(IMessageContextAccessor messageContextAccessor)
        {
            _messageContextAccessor = messageContextAccessor;
        }

        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            try
            {
                _messageContextAccessor.MessageContext = new MessageHandling.MessageContext(context.RawMessage, context.QueueUri, context.MessageAttributes);

                return await func(stoppingToken).ConfigureAwait(false);
            }
            finally
            {
                _messageContextAccessor.MessageContext = null;
            }
        }
    }
}
