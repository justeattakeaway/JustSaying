using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers
{
    public class TestHandler<T> : IHandlerAsync<T>
    {
        private readonly Action<T> _spy;

        public TestHandler(Action<T> spy)
        {
            _spy = spy;
        }

        public Task<bool> Handle(T testMessage)
        {
            _spy?.Invoke(testMessage);
            return Task.FromResult(true);
        }
    }
}
