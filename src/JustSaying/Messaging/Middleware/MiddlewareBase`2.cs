using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging.Middleware
{
    public abstract class MiddlewareBase<TContext, TOut>
    {
        private MiddlewareBase<TContext, TOut> _next;

        public MiddlewareBase<TContext, TOut> WithNext(MiddlewareBase<TContext, TOut> next)
        {
            _next = next;
            return this;
        }

        public Task<TOut> RunAsync(
            TContext context,
            Func<CancellationToken, Task<TOut>> func,
            CancellationToken stoppingToken)
        {
            return RunInnerAsync(context,
                async ct =>
                {
                    if (_next == null)
                    {
                        return await func(ct).ConfigureAwait(false);
                    }
                    else
                    {
                        return await _next.RunAsync(context, func, ct).ConfigureAwait(false);
                    }
                },
                stoppingToken);
        }

        protected abstract Task<TOut> RunInnerAsync(
            TContext context,
            Func<CancellationToken, Task<TOut>> func,
            CancellationToken stoppingToken);

        internal IEnumerable<string> Interrogate()
        {
            var thisType = GetType();
            if (thisType.IsGenericType && thisType.GetGenericTypeDefinition() == typeof(DelegateMiddleware<,>)) yield break;

            yield return GetType().Name;
            if (_next == null) yield break;

            foreach (var middlewareName in _next.Interrogate())
            {
                yield return middlewareName;
            }
        }
    }
}
