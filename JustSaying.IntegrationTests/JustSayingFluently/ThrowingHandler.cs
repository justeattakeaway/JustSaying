using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public class ThrowingHandler : IHandlerAsync<GenericMessage>
    {
        public ThrowingHandler()
        {
            DoneSignal = new TaskCompletionSource<object>();
        }

        public GenericMessage MessageReceived { get; set; }

        public TaskCompletionSource<object> DoneSignal { get; private set; }

        public async Task<bool> Handle(GenericMessage message)
        {
            MessageReceived = message;
            await Task.Delay(0);
            Tasks.DelaySendDone(DoneSignal);
            throw new TestException("ThrowingHandler has thrown");
        }
    }
}