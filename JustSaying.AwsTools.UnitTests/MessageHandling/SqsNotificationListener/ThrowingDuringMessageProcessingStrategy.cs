using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class ThrowingDuringMessageProcessingStrategy : IMessageProcessingStrategy
    {
        public int MaxBatchSize { get { return int.MaxValue; } }

        private readonly TaskCompletionSource<object> _doneSignal;

        public ThrowingDuringMessageProcessingStrategy(TaskCompletionSource<object> doneSignal)
        {
            _doneSignal = doneSignal;
        }

        public async Task BeforeGettingMoreMessages()
        {
            await Task.Delay(10);
        }

        public void ProcessMessage(Action action)
        {
            Fail();
        }

        private void Fail()
        {
            Patiently.DelaySendDone(_doneSignal);
            throw new Exception("Thrown by test ProcessMessage");
        }

    }
}