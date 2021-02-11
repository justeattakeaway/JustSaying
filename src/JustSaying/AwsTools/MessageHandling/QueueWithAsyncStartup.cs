using System.Threading.Tasks;

namespace JustSaying.AwsTools.MessageHandling
{
    /// <summary>
    /// Represents an <see cref="ISqsQueue"/> with an associated startup task that must be run before
    /// the queue is ready to be used.
    /// </summary>
    public class QueueWithAsyncStartup
    {
        /// <summary>
        /// Creates an instance of <see cref="QueueWithAsyncStartup"/> for which the <see cref="Queue"/>
        /// is already ready, and doesn't need to be initialised.
        /// </summary>
        /// <param name="queue">The queue that is ready.</param>
        public QueueWithAsyncStartup(ISqsQueue queue)
        {
            StartupTask = Task.CompletedTask;
            Queue = queue;
        }

        /// <summary>
        /// Creates an instance of <see cref="QueueWithAsyncStartup"/> that requires the <see cref="Queue"/>
        /// to be initialised by awaiting the <see cref="StartupTask"/>.
        /// </summary>
        /// <param name="startupTask">The <see cref="Task"/> that must be awaited on startup.</param>
        /// <param name="queue">An <see cref="ISqsQueue"/> that must be initialised on startup.</param>
        public QueueWithAsyncStartup(Task startupTask, ISqsQueue queue)
        {
            StartupTask = startupTask;
            Queue = queue;
        }

        /// <summary>
        /// A <see cref="Task"/> that must be run before the queue is ready.
        /// </summary>
        public Task StartupTask { get; }

        /// <summary>
        /// An <see cref="ISqsQueue"/> that will be ready when the <see cref="StartupTask"/>
        /// has been awaited.
        /// </summary>
        public ISqsQueue Queue { get; }
    }
}
