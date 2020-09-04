using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests.Support
{
    [ExactlyOnce(TimeOut = 5)]
    public class ExplicitExactlyOnceSignallingHandler : InspectableHandler<SimpleMessage>
    {
        private readonly TaskCompletionSource<object> _doneSignal;

        public ExplicitExactlyOnceSignallingHandler(TaskCompletionSource<object> doneSignal)
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
