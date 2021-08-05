using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.UnitTests.Messaging.Channels.Fakes
{
    public class FakeVisbilityUpdater : IMessageVisibilityUpdater
    {
        public Task UpdateMessageVisibilityTimeout(TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
