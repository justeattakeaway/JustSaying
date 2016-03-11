using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support
{
    [ExactlyOnce]
    public class ExactlyOnceSignallingHandler : IHandler<GenericMessage>
    {
        private readonly TaskCompletionSource<object> _doneSignal;

        public ExactlyOnceSignallingHandler(TaskCompletionSource<object> doneSignal)
        {
            _doneSignal = doneSignal;
        }

        public bool Handle(GenericMessage message)
        {
            HandleWasCalled = true;
            Tasks.DelaySendDone(_doneSignal);
            return true;
        }

        public bool HandleWasCalled { get; private set; }
    }
}