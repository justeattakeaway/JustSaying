using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;

namespace JustEat.Simples.NotificationStack.Messaging.MessageProcessingStrategies
{
    public class Throttled : IMessageProcessingStrategy
    {
        private readonly Func<int> _maximumAllowedMesagesInFlightProducer;
        private const int MinimumThreshold = 1;
        public int BlockingThreshold {
            get
            {
                var threshold = _maximumAllowedMesagesInFlightProducer() - _maximumBatchSize;
                if (threshold <= 0)
                    return MinimumThreshold;
                return threshold;
            }
           }

        private readonly int _maximumBatchSize;
        private readonly IMessageMonitor _messageMonitor;
        private readonly List<Task> _activeTasks;
        private long _activeTaskCount;

        public Throttled(int maximumAllowedMesagesInFlight, int maximumBatchSize, IMessageMonitor messageMonitor)
            : this(() => maximumAllowedMesagesInFlight, maximumBatchSize, messageMonitor)
        {}

        public Throttled(Func<int> maximumAllowedMesagesInFlightProducer, int maximumBatchSize,
            IMessageMonitor messageMonitor)
        {
            _maximumAllowedMesagesInFlightProducer = maximumAllowedMesagesInFlightProducer;
            _activeTasks = new List<Task>();
            _maximumBatchSize = maximumBatchSize;
            _messageMonitor = messageMonitor;
        }

        public void BeforeGettingMoreMessages()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            while (Interlocked.Read(ref _activeTaskCount) >= BlockingThreshold)
            {
                Task[] activeTasksToWaitOn;
                lock (_activeTasks)
                {
                    activeTasksToWaitOn = _activeTasks.Where(x => x != null).ToArray();
                }

                if (activeTasksToWaitOn.Length == 0)
                {
                    continue;
                }

                _messageMonitor.IncrementThrottlingStatistic();
                Task.WaitAny(activeTasksToWaitOn);
            }

            watch.Stop();

            _messageMonitor.HandleThrottlingTime(watch.ElapsedMilliseconds);
        }

        public void ProcessMessage(Action action)
        {
            var task = new Task(action);
            task.ContinueWith(MarkTaskAsComplete, TaskContinuationOptions.ExecuteSynchronously);
            
            Interlocked.Increment(ref _activeTaskCount);
            
            lock (_activeTasks)
            {
                _activeTasks.Add(task);
            }

            task.Start();
        }

        private void MarkTaskAsComplete(Task t)
        {
            lock (_activeTasks)
            {
                _activeTasks.Remove(t);
            }

            Interlocked.Decrement(ref _activeTaskCount);
        }
    }
}