using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests.TestHandlers
{
    namespace JustSaying.IntegrationTests.TestHandlers
    {
        public class BlockingOrderProcessor : IHandler<OrderPlaced>
        {

            public BlockingOrderProcessor()
            {
                DoneSignal = new TaskCompletionSource<object>();
            }

            public int ReceivedMessageCount { get; private set; }

            public TaskCompletionSource<object> DoneSignal { get; private set; }

            public bool Handle(OrderPlaced message)
            {
                ReceivedMessageCount++;
                Tasks.DelaySendDone(DoneSignal);
                return true;
            }
        }
    }

}
