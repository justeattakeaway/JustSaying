using JustSaying.Messaging.Channels.Context;

namespace JustSaying.UnitTests.Messaging.Channels.Fakes
{
    public class FakeMessageDeleter : IMessageDeleter
    {
        public Task DeleteMessage(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
