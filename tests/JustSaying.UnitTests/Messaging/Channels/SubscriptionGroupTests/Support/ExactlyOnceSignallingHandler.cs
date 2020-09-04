using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests.Support
{
    [ExactlyOnce]
    public class ExactlyOnceSignallingHandler : InspectableHandler<SimpleMessage>
    {
        private readonly TaskCompletionSource<object> _doneSignal;

        public ExactlyOnceSignallingHandler(TaskCompletionSource<object> doneSignal)
        {
            _doneSignal = doneSignal;
        }

        public override Task<bool> Handle(SimpleMessage message)
        {
            HandleWasCalled = true;
            TaskHelpers.DelaySendDone(_doneSignal);
            return Task.FromResult(true);
        }

        public bool HandleWasCalled { get; private set; }
    }
}
