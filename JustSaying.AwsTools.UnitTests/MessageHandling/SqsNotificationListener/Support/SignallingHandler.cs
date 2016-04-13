using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support
{
    public class SignallingHandler<T> : IHandler<T>
    {
        private readonly TaskCompletionSource<object> _doneSignal;
        private readonly IHandler<T> _inner;

        public SignallingHandler(TaskCompletionSource<object> doneSignal, IHandler<T> inner)
        {
            _doneSignal = doneSignal;
            _inner = inner;
        }

        public bool Handle(T message)
        {
            try
            {
                return _inner.Handle(message);
            }
            finally 
            {
                Tasks.DelaySendDone(_doneSignal);
            }
        }
    }
}
