using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Middleware.PostProcessing
{
    public class SqsPostProcessorMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            var succeeded = await func(stoppingToken);

            if (succeeded)
            {
                await context.MessageDeleter.DeleteMessage(stoppingToken);
            }

            return succeeded;
        }
    }
}
