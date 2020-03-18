using System;

namespace JustSaying.Messaging.Middleware
{
    public static class MiddlewareBuilder
    {
        public static MiddlewareBase<TContext, TOut> BuildAsync<TContext, TOut>(params Func<MiddlewareBase<TContext, TOut>, MiddlewareBase<TContext, TOut>>[] middleware)
        {
            MiddlewareBase<TContext, TOut> policy = new DelegateMiddleware<TContext, TOut>();
            return policy.WithAsync(middleware);
        }

        public static MiddlewareBase<TIn, TOut> WithAsync<TIn, TOut>(this MiddlewareBase<TIn, TOut> inner, params Func<MiddlewareBase<TIn, TOut>, MiddlewareBase<TIn, TOut>>[] middleware)
        {
            var policy = inner;

            foreach (var m in middleware)
            {
                policy = m(policy);
            }

            return policy;
        }
    }
}
