using System;
using System.Threading.Tasks;

namespace JustSaying.AwsTools
{
    // TODO Does this still meet async best-practice?

    internal sealed class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory) :
            base(() => Task.Factory.StartNew(valueFactory)) { }

        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap()) { }
    }
}
