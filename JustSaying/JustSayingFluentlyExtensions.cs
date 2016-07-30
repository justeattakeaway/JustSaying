namespace JustSaying
{
    public static class JustSayingFluentlyExtensions
    {
        public static IFluentSubscription IntoDefaultQueue(this ISubscriberIntoQueue subscriber)
        {
            return subscriber.IntoQueue(string.Empty);
        }
    }
}