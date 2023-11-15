using JustSaying.Messaging.Middleware;

namespace JustSaying.Fluent;

public sealed class QueueAddressConfiguration
{
    public string SubscriptionGroupName { get; set; }
    public Action<HandlerMiddlewareBuilder> MiddlewareConfiguration { get; set; }

    public void Validate()
    {
        // TODO
    }
}
