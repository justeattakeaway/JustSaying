using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener.Support
{
    public class ThrowingBeforeMessageProcessingStrategy : IMessageProcessingStrategy
    {
        public int MaxWorkers => int.MaxValue;

        public int AvailableWorkers
        {
            get
            {
                if (_firstTime)
                {
                    return 0;
                }

                return int.MaxValue;
            }
        }

        private readonly TaskCompletionSource<object> _doneSignal;
        private bool _firstTime = true;

        public ThrowingBeforeMessageProcessingStrategy(TaskCompletionSource<object> doneSignal)
        {
            _doneSignal = doneSignal;
        }

        public Task WaitForAvailableWorkers()
        {
            if (_firstTime)
            {
                _firstTime = false;
                Fail();
            }
            return Task.FromResult(true);
        }

        public Task StartWorker(Func<Task> action, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void Fail()
        {
            Tasks.DelaySendDone(_doneSignal);
            throw new TestException("Thrown by test ProcessMessage");
        }

    }
}
