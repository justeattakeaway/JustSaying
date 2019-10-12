using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.TestHandlers
{
    [ExactlyOnce]
    public class ExactlyOnceHandlerNoTimeout : ExactlyOnceHandlerWithTimeout
    {
    }
}
