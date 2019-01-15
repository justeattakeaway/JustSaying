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

        public Task WaitForAvailableWorkers()
        {
            return Task.CompletedTask;
        }

        public Task StartWorker(Func<Task> action, CancellationToken cancellationToken)
        {
            if (_firstTime)
            {
                _firstTime = false;
                return Fail();
            }

            return Task.CompletedTask;
        }

        private Task Fail()
        {
            TaskHelpers.DelaySendDone(_doneSignal);
            throw new TestException("Thrown by test ProcessMessage");
        }

    }
}
