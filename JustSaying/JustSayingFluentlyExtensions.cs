using System;
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
            if (handlers.Length == 0)
            {
                throw new ArgumentException("No handlers in list");
            }

            if (handlers.Length == 1)
            {
                sub.WithMessageHandler(handlers[0]);
            }

            var listHandler = new ListHandler<T>(handlers);
            return sub.WithMessageHandler(listHandler);
        }
    }
}