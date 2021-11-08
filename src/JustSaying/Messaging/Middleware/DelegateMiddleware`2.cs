namespace JustSaying.Messaging.Middleware
{
    internal class DelegateMiddleware<TContext, TOut> : MiddlewareBase<TContext, TOut>
    {
        protected override async Task<TOut> RunInnerAsync(TContext context, Func<CancellationToken, Task<TOut>> func, CancellationToken stoppingToken)
        {
            return await func(stoppingToken).ConfigureAwait(false);
        }
    }
}
