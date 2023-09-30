using JustSaying.Messaging.Middleware;

namespace JustSaying.TestingFramework;

public class TrackingMiddleware(string id, Action<string> onBefore, Action<string> onAfter) : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly Action<string> _onBefore = onBefore;
    private readonly Action<string> _onAfter = onAfter;
    private readonly string _id = id;

    protected override async Task<bool> RunInnerAsync(
        HandleMessageContext context,
        Func<CancellationToken,
            Task<bool>> func,
        CancellationToken stoppingToken)
    {
        _onBefore(_id);
        var result = await func(stoppingToken).ConfigureAwait(false);
        _onAfter(_id);

        return result;
    }
}
