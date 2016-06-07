using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support
{
    public class ThrowingDuringMessageProcessingStrategy : IMessageProcessingStrategy
    {
        public int MaxWorkers { get { return int.MaxValue; } }

        public int AvailableWorkers { get { return int.MaxValue; } }

        private readonly TaskCompletionSource<object> _doneSignal;
        private bool _firstTime = true;

        public ThrowingDuringMessageProcessingStrategy(TaskCompletionSource<object> doneSignal)
        {
            _doneSignal = doneSignal;
        }

        public async Task WaitForAvailableWorkers()
        {
            await Task.Yield();
        }

        public void StartWorker(Func<Task> action)
        {
            if (_firstTime)
            {
                _firstTime = false;
                Fail();
            }
        }

        private void Fail()
        {
            Tasks.DelaySendDone(_doneSignal);
            throw new TestException("Thrown by test ProcessMessage");
        }

    }
}