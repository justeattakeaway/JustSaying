using System;
using System.Linq;

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
        public static MiddlewareBase<TContext, TOut> BuildAsync<TContext, TOut>(
            params MiddlewareBase<TContext, TOut>[] middleware)
        {
            if (middleware == null) throw new ArgumentNullException(nameof(middleware));

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
        public static MiddlewareBase<TIn, TOut> WithAsync<TIn, TOut>(
            this MiddlewareBase<TIn, TOut> inner,
            params MiddlewareBase<TIn, TOut>[] middleware)
        {
            if (inner == null) throw new ArgumentNullException(nameof(inner));
            if (middleware == null) throw new ArgumentNullException(nameof(middleware));
            if (middleware.Any(x => x == null)) throw new ArgumentException("All provided middlewares should be non-null", nameof(middleware));

            var policy = inner;
            foreach (var m in middleware)
            {
                policy = m.WithNext(policy);
            }

            return policy;
        }
    }
}
