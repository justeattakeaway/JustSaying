using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers
{
    internal class FakeDispatcher : IMessageDispatcher
    {
        private readonly Action _spy;

        public FakeDispatcher(Action spy = null)
        {
            _spy = spy;
        }

        public Task DispatchMessageAsync(IQueueMessageContext messageContext, CancellationToken cancellationToken)
        {
            _spy?.Invoke();
            return Task.CompletedTask;
        }
    }
}
