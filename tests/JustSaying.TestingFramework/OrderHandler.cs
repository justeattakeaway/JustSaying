using JustSaying.Messaging.MessageHandling;

namespace JustSaying.TestingFramework
{
    public class OrderHandler : IHandlerAsync<Order>
    {
        public Task<bool> Handle(Order message)
            => Task.FromResult(true);
    }
}
