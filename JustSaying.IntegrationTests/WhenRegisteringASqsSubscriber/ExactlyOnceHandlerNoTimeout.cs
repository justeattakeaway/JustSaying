using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    [ExactlyOnce]
    public class ExactlyOnceHandlerNoTimeout : ExactlyOnceHandlerWithTimeout
    {
    }
}
