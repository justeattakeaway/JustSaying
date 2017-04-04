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

// we use the obsolete interface"IHandler<T>" here
#pragma warning disable 618
    [ExactlyOnce(TimeOut = 23)]
    public class OnceTestHandler : IHandler<GenericMessage>
#pragma warning restore 618
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
