using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Policies
{
    internal class NoopSqsPolicyAsync<T> : SqsPolicyAsync<T>
    {
        public NoopSqsPolicyAsync() : base(null)
        {
        }

        protected override async Task<T> RunInnerAsync(Func<Task<T>> func)
        {
            return await func().ConfigureAwait(false);
        }
    }
}
