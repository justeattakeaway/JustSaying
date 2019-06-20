using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener.Support
{
    public class ThrowingDuringMessageProcessingStrategy : IMessageProcessingStrategy
    {
        public int MaxWorkers => int.MaxValue;

        public int AvailableWorkers => int.MaxValue;

        private readonly TaskCompletionSource<object> _doneSignal;
        private bool _firstTime = true;

        public ThrowingDuringMessageProcessingStrategy(TaskCompletionSource<object> doneSignal)
        {
            _doneSignal = doneSignal;
        }

        public Task<int> WaitForAvailableWorkerAsync()
        {
            return Task.FromResult(AvailableWorkers);
        }

        public Task<bool> StartWorkerAsync(Func<Task> action, CancellationToken cancellationToken)
        {
            if (_firstTime)
            {
                _firstTime = false;
                return Fail();
            }

            return Task.FromResult(true);
        }

        private Task<bool> Fail()
        {
            TaskHelpers.DelaySendDone(_doneSignal);
            throw new TestException("Thrown by test ProcessMessage");
        }
    }
}
