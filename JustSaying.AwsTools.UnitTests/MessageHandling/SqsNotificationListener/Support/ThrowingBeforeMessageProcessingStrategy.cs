using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support
{
    public class ThrowingBeforeMessageProcessingStrategy : IMessageProcessingStrategy
    {
        public int MaxBatchSize { get { return int.MaxValue; } }

        private readonly TaskCompletionSource<object> _doneSignal;

        public ThrowingBeforeMessageProcessingStrategy(TaskCompletionSource<object> doneSignal)
        {
            _doneSignal = doneSignal;
        }

        public Task BeforeGettingMoreMessages()
        {
            Fail();
            return null;
        }

        public void ProcessMessage(Action action)
        {
        }

        private void Fail()
        {
            Tasks.DelaySendDone(_doneSignal);
            throw new Exception("Thrown by test ProcessMessage");
        }

    }
}