using System;
using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Middleware.Handle
{
    public class DelegateHandleMiddleware : MiddlewareBase<HandleMessageContext, bool>

    {
        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            return await func(stoppingToken);
        }
    }
}
