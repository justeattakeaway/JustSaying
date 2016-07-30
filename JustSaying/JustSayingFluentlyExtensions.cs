using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying
{
    public static class JustSayingFluentlyExtensions
    {
        public static IFluentSubscription IntoDefaultQueue(this ISubscriberIntoQueue subscriber)
        {
            return subscriber.IntoQueue(string.Empty);
        }

        public static IHaveFulfilledSubscriptionRequirements WithMessageHandlers<T>(
             this IFluentSubscription sub, params IHandlerAsync<T>[] handlers) where T : Message
        {
            var listHandler = new ListHandler<T>(handlers);
            return sub.WithMessageHandler(listHandler);
        }
    }
}