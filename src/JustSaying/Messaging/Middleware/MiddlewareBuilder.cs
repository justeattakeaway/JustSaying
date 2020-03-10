using System;

namespace JustSaying.Messaging.Middleware
{
    public static class MiddlewareBuilder
    {
        public static MiddlewareBase<TContext, TOut> BuildAsync<TContext, TOut>(params Func<MiddlewareBase<TContext, TOut>, MiddlewareBase<TContext, TOut>>[] policies)
        {
            MiddlewareBase<TContext, TOut> policy = new NoopMiddleware<TContext, TOut>();
            return policy.WithAsync(policies);
        }

        public static MiddlewareBase<TIn, TOut> WithAsync<TIn, TOut>(this MiddlewareBase<TIn, TOut> inner, params Func<MiddlewareBase<TIn, TOut>, MiddlewareBase<TIn, TOut>>[] policies)
        {
            var policy = inner;

            foreach (var p in policies)
            {
                policy = p(policy);
            }

            return policy;
        }
    }
}
