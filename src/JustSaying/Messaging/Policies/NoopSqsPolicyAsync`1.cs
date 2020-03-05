using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Policies
{
    internal class InnerSqsPolicyAsync<T> : SqsPolicyAsync<T>
    {
        public InnerSqsPolicyAsync() : base(null)
        {
        }

        protected override async Task<T> RunInnerAsync(Func<Task<T>> func)
        {
            return await func().ConfigureAwait(false);
        }
    }
}
