using System.Threading.Tasks;

namespace JustSaying.AwsTools.MessageHandling
{
    public class QueueWithAsyncStartup<TQueue> where TQueue : ISqsQueue
    {
        public QueueWithAsyncStartup(TQueue queue)
        {
            StartupTask = Task.CompletedTask;
            Queue = queue;
        }

        public QueueWithAsyncStartup(Task startupTask, TQueue queue)
        {
            StartupTask = startupTask;
            Queue = queue;
        }

        public Task StartupTask { get; }
        public TQueue Queue { get; }
    }
}
