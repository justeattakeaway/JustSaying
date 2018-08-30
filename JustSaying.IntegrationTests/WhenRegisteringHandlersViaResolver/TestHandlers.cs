using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class TestMessage : Message
    {
    }

    public class HandlerA : IHandlerAsync<TestMessage>
    {
        public Task<bool> Handle(TestMessage message) => Task.FromResult(true);
    }

    public class HandlerB : IHandlerAsync<TestMessage>
    {
        public Task<bool> Handle(TestMessage message) => Task.FromResult(true);
    }

    public class HandlerC : IHandlerAsync<TestMessage>
    {
        public Task<bool> Handle(TestMessage message) => Task.FromResult(true);
    }
}
