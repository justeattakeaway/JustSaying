using JustSaying.Extensions;

namespace JustSaying.Messaging.Middleware;

public abstract class MiddlewareBase<TContext, TOut>
{
    private MiddlewareBase<TContext, TOut> _next;

    public MiddlewareBase<TContext, TOut> WithNext(MiddlewareBase<TContext, TOut> next)
    {
        _next = next;
        return this;
    }

    internal bool HasNext => _next != null;

    public async Task<TOut> RunAsync(
        TContext context,
        Func<CancellationToken, Task<TOut>> func,
        CancellationToken stoppingToken)
    {
        return await RunInnerAsync(context,
            async ct =>
            {
                if (_next != null)
                {
                    return await _next.RunAsync(context, func, ct).ConfigureAwait(false);
                }

                if (func != null)
                {
                    return await func(ct).ConfigureAwait(false);
                }

                else return default;
            },
            stoppingToken).ConfigureAwait(false);
    }

    protected abstract Task<TOut> RunInnerAsync(
        TContext context,
        Func<CancellationToken, Task<TOut>> func,
        CancellationToken stoppingToken);

    internal IEnumerable<string> Interrogate()
    {
        var thisType = GetType();
        if (thisType.IsGenericType && thisType.GetGenericTypeDefinition() == typeof(DelegateMiddleware<,>)) yield break;

        yield return GetType().ToSimpleName();
        if (_next == null) yield break;

        foreach (var middlewareName in _next.Interrogate())
        {
            yield return middlewareName;
        }
    }
}
