using JustSaying.Fluent;
using JustSaying.Models;

namespace JustSaying.IntegrationTests.Fluent;

internal static class MessagingBusBuilderTestExtensions
{
    public static MessagingBusBuilder WithLoopbackQueue<T>(
        this MessagingBusBuilder builder,
        string name,
        Action<QueueSubscriptionBuilder<T>> configure = null)
        where T : Message
    {
        return builder
            .Publications((options) => options.WithQueue<T>(o => o.WithName(name)))
            .Subscriptions((options) => options.ForQueue<T>(subscriptionBuilder =>
            {
                subscriptionBuilder.WithQueueName(name);
                configure?.Invoke(subscriptionBuilder);
            }));
    }

    public static MessagingBusBuilder WithLoopbackFifoQueue<T>(
        this MessagingBusBuilder builder,
        string name,
        Action<QueueSubscriptionBuilder<T>> configure = null)
        where T : Message
    {
        return builder
            .Publications((options) => options.WithQueue<T>(o => o.WithName(name).WithFifo()))
            .Subscriptions((options) => options.ForQueue<T>(subscriptionBuilder =>
            {
                subscriptionBuilder.WithQueueName(name).WithFifo();
                configure?.Invoke(subscriptionBuilder);
            }));
    }

    public static MessagingBusBuilder WithLoopbackTopic<T>(
        this MessagingBusBuilder builder,
        string name,
        Action<TopicSubscriptionBuilder<T>> configure = null)
        where T : Message
    {
        return builder
            .Publications((options) => options.WithTopic<T>(publicationBuilder =>
                publicationBuilder.WithTopicName(name)
            ))
            .Subscriptions((options) => options.ForTopic<T>(subscriptionBuilder =>
            {
                subscriptionBuilder.WithQueueName(name).WithTopicName(name);
                configure?.Invoke(subscriptionBuilder);
            }));
    }

    public static MessagingBusBuilder WithLoopbackFifoTopic<T>(
        this MessagingBusBuilder builder,
        string name,
        Action<TopicSubscriptionBuilder<T>> configure = null)
        where T : Message
    {
        return builder
            .Publications((options) => options.WithTopic<T>(publicationBuilder =>
                publicationBuilder.WithTopicName(name).WithFifo()
            ))
            .Subscriptions((options) => options.ForTopic<T>(subscriptionBuilder =>
            {
                subscriptionBuilder.WithQueueName(name).WithTopicName(name).WithFifo();
                configure?.Invoke(subscriptionBuilder);
            }));
    }
}
