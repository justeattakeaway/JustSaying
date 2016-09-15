using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling
{
    public class UnadornedHandlerAsync : IHandlerAsync<GenericMessage>
    {
        public Task<bool> Handle(GenericMessage message)
        {
            return Task.FromResult(true);
        }
    }

    [ExactlyOnce(TimeOut = 42)]
    public class OnceTestHandlerAsync : IHandlerAsync<GenericMessage>
    {
        public Task<bool> Handle(GenericMessage message)
        {
            return Task.FromResult(true);
        }
    }

    [ExactlyOnce(TimeOut = 23)]
    public class OnceTestHandler : IHandler<GenericMessage>
    {
        public bool Handle(GenericMessage message)
        {
            return true;
        }
    }

    [ExactlyOnce]
    public class OnceHandlerWithImplicitTimeoutAsync : IHandlerAsync<GenericMessage>
    {
        public Task<bool> Handle(GenericMessage message)
        {
            return Task.FromResult(true);
        }
    }
}
