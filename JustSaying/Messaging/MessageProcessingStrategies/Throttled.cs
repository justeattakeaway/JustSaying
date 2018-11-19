using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Known issue")]
    public class Throttled : IMessageProcessingStrategy
    {
        private readonly IMessageMonitor _messageMonitor;
        private readonly SemaphoreSlim _semaphore;

        public Throttled(int maxWorkers, IMessageMonitor messageMonitor)
        {
            _messageMonitor = messageMonitor;
            MaxWorkers = maxWorkers;
            _semaphore = new SemaphoreSlim(maxWorkers, maxWorkers);
        }

        public void StartWorker(Func<Task> action, CancellationToken cancellationToken)
        {
            var messageProcessingTask = new Task<Task>(() => ReleaseOnCompleted(action));

            try
            {
                _semaphore.Wait(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            messageProcessingTask.Start();
        }

        private async Task ReleaseOnCompleted(Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }


        public async Task WaitForAvailableWorkers()
        {
            if (_semaphore.CurrentCount != 0)
                return;

            _messageMonitor.IncrementThrottlingStatistic();

            var watch = Stopwatch.StartNew();
            await AsTask(_semaphore.AvailableWaitHandle).ConfigureAwait(false);
            watch.Stop();

            _messageMonitor.HandleThrottlingTime(watch.ElapsedMilliseconds);
        }

        private static Task AsTask(WaitHandle waitHandle)
        {
            var tcs = new TaskCompletionSource<object>();

            ThreadPool.RegisterWaitForSingleObject(
                waitObject: waitHandle,
                callBack: (o, timeout) => { tcs.SetResult(null); },
                state: null,
                timeout: TimeSpan.FromMilliseconds(int.MaxValue),
                executeOnlyOnce: true);

            return tcs.Task;
        }

        public int MaxWorkers { get; }
        public int AvailableWorkers
        {
            get { return _semaphore.CurrentCount; }
        }
    }
}
