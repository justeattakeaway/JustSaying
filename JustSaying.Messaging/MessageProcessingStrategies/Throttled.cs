using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public class Throttled : IMessageProcessingStrategy
    {
        private readonly Func<int> _maxWorkersProducer;

        private readonly IMessageMonitor _messageMonitor;
        private readonly List<Task> _activeTasks;

        public Throttled(int maxWorkers, IMessageMonitor messageMonitor)
            : this(() => maxWorkers, messageMonitor)
        {}

        public Throttled(Func<int> maxWorkersProducer,
            IMessageMonitor messageMonitor)
        {
            _maxWorkersProducer = maxWorkersProducer;
            _messageMonitor = messageMonitor;

            _activeTasks = new List<Task>(_maxWorkersProducer());
        }

        public int MaxWorkers
        {
            get { return _maxWorkersProducer(); }
        }

        public int AvailableWorkers
        {
            get
            {
                int activeTaskCount;
                lock (_activeTasks)
                {
                    activeTaskCount = _activeTasks.Count;
                }
                var freeTasks = _maxWorkersProducer() - activeTaskCount;
                return Math.Max(freeTasks, 0);
            }
        }

        public async Task WaitForAvailableWorkers()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // wait for some tasks to complete
            while (AvailableWorkers == 0)
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

        public void StartWorker(Func<Task> action)
        {
            // task is named but not yet started
            var messageProcessingTask = new Task<Task>(action);

            // what happens when it ends
            messageProcessingTask.Unwrap()
                .ContinueWith(t => MarkTaskAsCompleted(messageProcessingTask), TaskContinuationOptions.ExecuteSynchronously);

            // start it
            MarkTaskAsActive(messageProcessingTask);
            messageProcessingTask.Start();
        }

        private void MarkTaskAsActive(Task t)
        {
            lock (_activeTasks)
            {
                _activeTasks.Add(t);
            }
        }

        private void MarkTaskAsCompleted(Task t)
        {
            lock (_activeTasks)
            {
                if (!_activeTasks.Contains(t))
                {
                    throw new InvalidOperationException("Cannot find task in task list " + t.Id);
                }

                _activeTasks.Remove(t);
            }
        }
    }
}