using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriberBusTests.Support
{
    [ExactlyOnce]
    public class ExactlyOnceSignallingHandler : IHandlerAsync<SimpleMessage>
    {
        private readonly TaskCompletionSource<object> _doneSignal;

        public ExactlyOnceSignallingHandler(TaskCompletionSource<object> doneSignal)
        {
            _doneSignal = doneSignal;
        }

        public Task<bool> Handle(SimpleMessage message)
        {
            HandleWasCalled = true;
            TaskHelpers.DelaySendDone(_doneSignal);
            return Task.FromResult(true);
        }

        public bool HandleWasCalled { get; private set; }
    }
}
