namespace JustSaying.Messaging.Middleware.PostProcessing
{
    /// <summary>
    /// A middleware that provides post-processing of messages after they're handled, such as deleting
    /// messages from the queue after being successfully processed.
    /// </summary>
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
