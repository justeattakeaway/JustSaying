using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.AwsTools.MessageHandling
{
    public class UnadornedHandlerAsync : IHandlerAsync<SimpleMessage>
    {
        public Task<bool> Handle(SimpleMessage message)
        {
            return Task.FromResult(true);
        }
    }

    [ExactlyOnce(TimeOut = 42)]
    public class OnceTestHandlerAsync : IHandlerAsync<SimpleMessage>
    {
        public Task<bool> Handle(SimpleMessage message)
        {
            return Task.FromResult(true);
        }
    }

    [ExactlyOnce]
    public class OnceHandlerWithImplicitTimeoutAsync : IHandlerAsync<SimpleMessage>
    {
        public Task<bool> Handle(SimpleMessage message)
        {
            return Task.FromResult(true);
        }
    }
}
