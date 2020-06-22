using System;

namespace JustSaying.Messaging.Middleware
{
    /// <summary>
    /// Helper methods to chain instances of <see cref="MiddlewareBase{TContext, TOut}" />.
    /// </summary>
    public static class MiddlewareBuilder
    {
        /// <summary>
        /// Chains instances of <see cref="MiddlewareBase{TContext, TOut}"/> together using
        /// <see cref="DelegateMiddleware{TContext, TOut}"/> as the inner middleware.
        /// </summary>
        /// <param name="middleware">The instances of <see cref="MiddlewareBase{TContext, TOut}"/> to add to the
        /// returned composite <see cref="MiddlewareBase{TContext, TOut}"/>.</param>
        /// <returns>A composite <see cref="MiddlewareBase{TContext, TOut}"/>.</returns>
        public static MiddlewareBase<TContext, TOut> BuildAsync<TContext, TOut>(params Func<MiddlewareBase<TContext, TOut>, MiddlewareBase<TContext, TOut>>[] middleware)
        {
            MiddlewareBase<TContext, TOut> policy = new DelegateMiddleware<TContext, TOut>();
            return policy.WithAsync(middleware);
        }

        /// <summary>
        /// Chains instances of <see cref="MiddlewareBase{TContext, TOut}"/> together using
        /// the given inner as the inner middleware.
        /// </summary>
        /// <param name="inner">The innermost instance of <see cref="MiddlewareBase{TContext, TOut}"/>.</param>
        /// <param name="middleware">The instances of <see cref="MiddlewareBase{TContext, TOut}"/> to add to the
        /// returned composite <see cref="MiddlewareBase{TContext, TOut}"/>.</param>
        /// <returns>A composite <see cref="MiddlewareBase{TContext, TOut}"/>.</returns>
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
