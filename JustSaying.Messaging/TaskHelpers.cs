using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace JustSaying.Messaging
{
    public static class TaskHelpers
    {
        public static ConfiguredTaskAwaitable<T> OnAnyThread<T>(this Task<T> task)
        {
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }

        public static ConfiguredTaskAwaitable OnAnyThread(this Task task)
        {
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
