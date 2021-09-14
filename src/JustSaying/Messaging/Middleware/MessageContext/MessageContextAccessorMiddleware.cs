using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.Middleware.MessageContext
{
    /// <summary>
    /// A middleware that sets context that is available in message handlers by resolving an `IMessageContextAccessor`.
    /// </summary>
    public class MessageContextAccessorMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly IMessageContextAccessor _messageContextAccessor;

        /// <summary>
        /// Creates an instance of <see cref="MessageContextAccessorMiddleware"/>.
        /// </summary>
        /// <param name="messageContextAccessor">The <see cref="IMessageContextAccessor"/> to set.</param>
        public MessageContextAccessorMiddleware(IMessageContextAccessor messageContextAccessor)
        {
            _messageContextAccessor = messageContextAccessor ?? throw new ArgumentNullException(nameof(messageContextAccessor));
        }

        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            _messageContextAccessor.MessageContext = new MessageHandling.MessageContext(context.RawMessage, context.QueueUri, context.MessageAttributes);

            try
            {
                return await func(stoppingToken).ConfigureAwait(false);
            }
            finally
            {
                _messageContextAccessor.MessageContext = null;
            }
        }
    }
}
