using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public class Throttled : IMessageProcessingStrategy
    {
        private readonly Func<int> _maximumAllowedMesagesInFlightProducer;

        private readonly IMessageMonitor _messageMonitor;
        private readonly List<Task> _activeTasks;
        private long _activeTaskCount;

        public Throttled(int maximumAllowedMesagesInFlight, IMessageMonitor messageMonitor)
            : this(() => maximumAllowedMesagesInFlight, messageMonitor)
        {}

        public Throttled(Func<int> maximumAllowedMesagesInFlightProducer,
            IMessageMonitor messageMonitor)
        {
            _maximumAllowedMesagesInFlightProducer = maximumAllowedMesagesInFlightProducer;
            _activeTasks = new List<Task>();
            _messageMonitor = messageMonitor;
        }

        public int FreeTasks
        {
            get
            {
                var inUse = Interlocked.Read(ref _activeTaskCount);
                int freeTasks = _maximumAllowedMesagesInFlightProducer() - (int)inUse;
                return Math.Max(freeTasks, 0);
            }
        }

        public async Task AwaitAtLeastOneTaskToComplete()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            // wait for some tasks to complete
            while (FreeTasks == 0)
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
                await Task.WhenAny(activeTasksToWaitOn);
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