using System;
using System.Threading.Tasks;
using JustSaying.Messaging.Policies;

namespace JustSaying.UnitTests.Messaging.Policies.ExamplePolicies
{
    public class ErrorHandlingSqsPolicyAsync<T, TException> : SqsPolicyAsync<T>
        where TException : Exception
    {
        public ErrorHandlingSqsPolicyAsync(SqsPolicyAsync<T> next) : base(next)
        {

        }

        protected override async Task<T> RunInnerAsync(Func<Task<T>> func)
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
