using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests.Support
{
    [ExactlyOnce]
    public class ExactlyOnceSignallingHandler : InspectableHandler<SimpleMessage>
    {
    }
}
