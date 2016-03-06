using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    [ExactlyOnce(TimeOut = 5)]
    public class ExactlyOnceSignallingHandler : IHandler<GenericMessage>
    {
        private bool _handlerWasCalled;
        private readonly TaskCompletionSource<object> _doneSignal;

        public ExactlyOnceSignallingHandler(TaskCompletionSource<object> doneSignal)
        {
            _doneSignal = doneSignal;
        }

        public bool Handle(GenericMessage message)
        {
            _handlerWasCalled = true;
            Patiently.DelaySendDone(_doneSignal);
            return true;
        }

        public bool HandlerWasCalled()
        {
            return _handlerWasCalled;
        }
    }
}