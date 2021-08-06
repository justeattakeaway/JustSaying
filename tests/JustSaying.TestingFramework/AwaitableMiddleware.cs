using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Middleware;

namespace JustSaying.TestingFramework
{
    public class AwaitableMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        public Task Complete { get; private set; }

        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            var tcs = new TaskCompletionSource();
            Complete = tcs.Task;
            try
            {
                return await func(stoppingToken);
            }
            finally
            {
                tcs.SetResult();
            }
        }
    }
}
