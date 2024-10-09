using JustSaying.Fluent;
using JustSaying.Models;

namespace JustSaying.IntegrationTests.Fluent;

internal static class MessagingBusBuilderTestExtensions
{
    public static MessagingBusBuilder WithLoopbackQueue<T>(this MessagingBusBuilder builder, string name,
        Action<QueueSubscriptionBuilder<T>> configure = null)
        where T : Message
    {
        return builder
            .Publications((options) => options.WithQueue<T>(o => o.WithQueueName(name)))
            .Subscriptions((options) => options.ForQueue<T>(subscriptionBuilder =>
            {
                subscriptionBuilder.WithQueueName(name);
                configure?.Invoke(subscriptionBuilder);
            }));
    }

    public static MessagingBusBuilder WithLoopbackTopic<T>(this MessagingBusBuilder builder, string name,
        Action<TopicSubscriptionBuilder<T>> configure = null)
        where T : Message
    {
        return builder
            .Publications((options) => options.WithTopic<T>())
            .Subscriptions((options) => options.ForTopic<T>(subscriptionBuilder =>
            {
                subscriptionBuilder.WithQueueName(name);
                configure?.Invoke(subscriptionBuilder);
            }));
    }

    public static MessagingBusBuilder WithLoopbackQueueAndPublicationOptions<T>(this MessagingBusBuilder builder, string name,
        Action<QueuePublicationBuilder<T>> configurePublisher = null, Action<QueueSubscriptionBuilder<T>> configureSubscriber = null)
        where T : Message
    {
        return builder
            .Publications((options) => options.WithQueue<T>(publicationBuilder =>
            {
                publicationBuilder.WithQueueName(name);
                configurePublisher?.Invoke(publicationBuilder);
            }))
            .Subscriptions((options) => options.ForQueue<T>(subscriptionBuilder =>
            {
                subscriptionBuilder.WithQueueName(name);
                configureSubscriber?.Invoke(subscriptionBuilder);
            }));
    }

    public static MessagingBusBuilder WithLoopbackTopicAndPublicationOptions<T>(this MessagingBusBuilder builder, string name,
        Action<TopicPublicationBuilder<T>> configure)
        where T : Message
    {
        return builder
            .Publications((options) => options.WithTopic(configure))
            .Subscriptions((options) => options.ForTopic<T>(subscriptionBuilder =>
            {
                subscriptionBuilder.WithQueueName(name);
            }));
    }
}
