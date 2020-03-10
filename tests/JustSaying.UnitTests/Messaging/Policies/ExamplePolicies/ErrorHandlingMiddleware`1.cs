using System;
using System.Threading.Tasks;
using JustSaying.Messaging.Middleware;

namespace JustSaying.UnitTests.Messaging.Policies.ExamplePolicies
{
    public class ErrorHandlingMiddleware<TContext, TOut, TException> : MiddlewareBase<TContext, TOut>
        where TException : Exception
    {
        public ErrorHandlingMiddleware(MiddlewareBase<TContext, TOut> next) : base(next)
        {

        }

        protected override async Task<TOut> RunInnerAsync(TContext context, Func<Task<TOut>> func)
        {
            try
            {
                return await func();
            }
            catch (TException)
            {
                return default;
            }
        }
    }
}
