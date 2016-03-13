using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support
{
    public class ThrowingBeforeMessageProcessingStrategy : IMessageProcessingStrategy
    {
        public int MaxBatchSize { get { return int.MaxValue; } }

        private readonly TaskCompletionSource<object> _doneSignal;
        private bool _firstTime = true;

        public ThrowingBeforeMessageProcessingStrategy(TaskCompletionSource<object> doneSignal)
        {
            _doneSignal = doneSignal;
        }

        public Task BeforeGettingMoreMessages()
        {
            if (_firstTime)
            {
                _firstTime = false;
                Fail();
            }
            return null;
        }

        public void ProcessMessage(Action action)
        {
        }

        private void Fail()
        {
            Tasks.DelaySendDone(_doneSignal);
            throw new TestException("Thrown by test ProcessMessage");
        }

    }
}