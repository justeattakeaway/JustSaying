using System;
using System.Threading.Tasks;

namespace JustSaying.AwsTools
{
    internal sealed class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(taskFactory) { }
    }
}
