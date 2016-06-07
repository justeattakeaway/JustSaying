using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support
{
    public class ThrowingBeforeMessageProcessingStrategy : IMessageProcessingStrategy
    {
        public int MaxWorkers
        {
            get { return int.MaxValue; }
        }

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

        public void StartWorker(Func<Task> action)
        {
        }

        private void Fail()
        {
            Tasks.DelaySendDone(_doneSignal);
            throw new TestException("Thrown by test ProcessMessage");
        }

    }
}