using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support
{
    public class SignallingHandler<T> : IAsyncHandler<T>
    {
        private readonly TaskCompletionSource<object> _doneSignal;
        private readonly IAsyncHandler<T> _inner;

        public SignallingHandler(TaskCompletionSource<object> doneSignal, IAsyncHandler<T> inner)
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
                Tasks.DelaySendDone(_doneSignal);
            }
        }
    }
}
