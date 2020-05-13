using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriberBusTests.Support
{
    public class SignallingHandler<T> : IHandlerAsync<T>
    {
        private readonly TaskCompletionSource<object> _doneSignal;
        private readonly IHandlerAsync<T> _inner;

        public SignallingHandler(TaskCompletionSource<object> doneSignal, IHandlerAsync<T> inner)
        {
            _doneSignal = doneSignal;
            _inner = inner;
        }

        public async Task<bool> Handle(T message)
        {
            try
            {
                return await _inner.Handle(message);
            }
            finally
            {
                TaskHelpers.DelaySendDone(_doneSignal);
            }
        }
    }
}
