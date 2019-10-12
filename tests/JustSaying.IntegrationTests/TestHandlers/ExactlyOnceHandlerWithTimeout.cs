using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests.TestHandlers
{
    [ExactlyOnce(TimeOut = 10)]
    public class ExactlyOnceHandlerWithTimeout : IHandlerAsync<SimpleMessage>
    {
        private int _count;

        public Task<bool> Handle(SimpleMessage message)
        {
            Interlocked.Increment(ref _count);
            return Task.FromResult(true);
        }

        public int NumberOfTimesIHaveBeenCalled()
        {
            return _count;
        }
    }
}
