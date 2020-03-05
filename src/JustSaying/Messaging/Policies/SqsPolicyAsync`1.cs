using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Policies
{
    public abstract class SqsPolicyAsync<T>
    {
        private readonly SqsPolicyAsync<T> _next;

        public SqsPolicyAsync(SqsPolicyAsync<T> next)
        {
            _next = next;
        }

        public async Task<T> RunAsync(Func<Task<T>> func)
        {
            return await RunInnerAsync(async () =>
            {
                if (_next == null)
                {
                    return await func().ConfigureAwait(false);
                }
                else
                {
                    return await _next.RunAsync(func).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        protected abstract Task<T> RunInnerAsync(Func<Task<T>> func);
    }
}
