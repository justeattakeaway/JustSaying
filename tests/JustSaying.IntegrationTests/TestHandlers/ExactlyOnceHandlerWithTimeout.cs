using System.Collections.Concurrent;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests.TestHandlers
{
    [ExactlyOnce(TimeOut = 10)]
    public class ExactlyOnceHandlerWithTimeout : IHandlerAsync<SimpleMessage>
    {
        private readonly ConcurrentDictionary<string, int> _counts = new ConcurrentDictionary<string, int>();

        public Task<bool> Handle(SimpleMessage message)
        {
            _counts.AddOrUpdate(message.UniqueKey(), 1, (_, i) => i + 1);
            return Task.FromResult(true);
        }

        public int NumberOfTimesIHaveBeenCalledForMessage(string id)
        {
            if (!_counts.TryGetValue(id, out int count))
            {
                count = 0;
            }

            return count;
        }
    }
}
