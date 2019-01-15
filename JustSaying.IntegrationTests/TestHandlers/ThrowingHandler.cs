using System.Threading.Tasks;
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

        public async Task<bool> Handle(SimpleMessage message)
        {
            MessageReceived = message;

            await Task.Delay(0);
            TaskHelpers.DelaySendDone(DoneSignal);

            throw new TestException("ThrowingHandler has thrown");
        }
    }
}
