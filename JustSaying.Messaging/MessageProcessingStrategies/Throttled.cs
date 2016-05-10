using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public class Throttled : IMessageProcessingStrategy
    {
        private readonly Func<int> _maximumAllowedMesagesInFlightProducer;

        private readonly IMessageMonitor _messageMonitor;
        private readonly List<Task> _activeTasks = new List<Task>();

        public Throttled(int maximumAllowedMesagesInFlight, IMessageMonitor messageMonitor)
            : this(() => maximumAllowedMesagesInFlight, messageMonitor)
        {}

        public Throttled(Func<int> maximumAllowedMesagesInFlightProducer,
            IMessageMonitor messageMonitor)
        {
            _maximumAllowedMesagesInFlightProducer = maximumAllowedMesagesInFlightProducer;
            _messageMonitor = messageMonitor;
        }

        public int FreeTasks
        {
            get
            {
                int activeTaskCount;
                lock (_activeTasks)
                {
                    activeTaskCount = _activeTasks.Count;
                }
                var freeTasks = _maximumAllowedMesagesInFlightProducer() - activeTaskCount;
                return Math.Max(freeTasks, 0);
            }
        }

        public async Task AwaitAtLeastOneTaskToComplete()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

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

        public void ProcessMessage(Func<Task> action)
        {
            // name it
            var messageProcessingTask = new Task<Task>(action);

            // what happens when it ends
            messageProcessingTask.Unwrap()
                .ContinueWith(t => MarkTaskAsCompleted(messageProcessingTask), TaskContinuationOptions.ExecuteSynchronously);

            MarkTaskAsActive(messageProcessingTask);

            // actually start it
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