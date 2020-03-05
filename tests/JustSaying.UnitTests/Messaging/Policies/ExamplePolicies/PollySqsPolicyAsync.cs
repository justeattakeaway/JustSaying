using System;
using System.Threading.Tasks;
using JustSaying.Messaging.Policies;
using Polly;

namespace JustSaying.UnitTests.Messaging.Policies.ExamplePolicies
{
    public class PollySqsPolicyAsync<T> : SqsPolicyAsync<T>
    {
        private readonly IAsyncPolicy _policy;

        public PollySqsPolicyAsync(SqsPolicyAsync<T> next, IAsyncPolicy policy) : base(next)
        {
            _policy = policy;
        }

        protected override async Task<T> RunInnerAsync(Func<Task<T>> func)
        {
            return await _policy.ExecuteAsync(func);
        }
    }
}
