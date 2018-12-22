using JustSaying.Models;

namespace JustSaying.IntegrationTests.Fluent
{
    internal static class MessagingBusBuilderTestExtensions
    {
        public static MessagingBusBuilder WithLoopbackQueue<T>(this MessagingBusBuilder builder, string name)
            where T : Message
        {
            return builder
                .Publications((options) => options.WithQueue<T>(name))
                .Subscriptions((options) => options.ForQueue<T>(name));
        }

        public static MessagingBusBuilder WithLoopbackTopic<T>(this MessagingBusBuilder builder, string name)
            where T : Message
        {
            return builder
                .Publications((options) => options.WithTopic<T>())
                .Subscriptions((options) => options.ForTopic<T>(name));
        }
    }
}
