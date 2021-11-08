using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests.TestHandlers
{
    public class ThrowingHandler : IHandlerAsync<SimpleMessage>
    {
        public ThrowingHandler()
        {
            DoneSignal = new TaskCompletionSource<object>();
        }

        public SimpleMessage MessageReceived { get; set; }

        public TaskCompletionSource<object> DoneSignal { get; private set; }

        public Task<bool> Handle(SimpleMessage message)
        {
            MessageReceived = message;
            throw new TestException("ThrowingHandler has thrown");
        }
    }
}
