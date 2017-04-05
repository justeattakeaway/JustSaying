using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support
{
    [ExactlyOnce]
    public class ExactlyOnceSignallingHandler : IHandlerAsync<GenericMessage>
    {
        private readonly TaskCompletionSource<object> _doneSignal;

        public ExactlyOnceSignallingHandler(TaskCompletionSource<object> doneSignal)
        {
            _doneSignal = doneSignal;
        }

        public Task<bool> Handle(GenericMessage message)
        {
            HandleWasCalled = true;
            Tasks.DelaySendDone(_doneSignal);
            return Task.FromResult(true);
        }

        public bool HandleWasCalled { get; private set; }
    }
}