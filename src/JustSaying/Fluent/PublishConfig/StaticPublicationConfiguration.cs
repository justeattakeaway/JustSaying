using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using Microsoft.Extensions.Logging;

#pragma warning disable CS0618

namespace JustSaying.Fluent;

internal sealed class StaticPublicationConfiguration<TMessage> : ITopicPublisher<TMessage>  where TMessage : class
{
    public Func<CancellationToken, Task> StartupTask { get; }
    public IMessagePublisher<TMessage> Publisher { get; }

    public StaticPublicationConfiguration(
        Func<CancellationToken, Task> startupTask,
        IMessagePublisher<TMessage> publisher)
    {
        StartupTask = startupTask;
        Publisher = publisher;
    }

    public static StaticPublicationConfiguration<TMessage> Build(
        string topicName,
        Dictionary<string, string> tags,
        SnsWriteConfiguration writeConfiguration,
        IAmazonSimpleNotificationService snsClient,
        ILoggerFactory loggerFactory,
        JustSayingBus bus)
    {
        var readConfiguration = new SqsReadConfiguration(SubscriptionType.ToTopic)
        {
            TopicName = topicName
        };

        readConfiguration.ApplyTopicNamingConvention<TMessage>(bus.Config.TopicNamingConvention);

        var eventPublisher = new SnsMessagePublisher<TMessage>(
            snsClient,
            bus.SerializationRegister,
            loggerFactory,
            bus.Config.MessageSubjectProvider)
        {
            MessageResponseLogger = bus.Config.MessageResponseLogger,
        };

        var snsTopic = new SnsTopicByName(
            readConfiguration.TopicName,
            snsClient,
            loggerFactory)
        {
            Tags = tags
        };

        async Task StartupTask(CancellationToken cancellationToken)
        {
            if (writeConfiguration.Encryption != null)
            {
                await snsTopic.CreateWithEncryptionAsync(writeConfiguration.Encryption, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await snsTopic.CreateAsync(cancellationToken).ConfigureAwait(false);
            }

            await snsTopic.EnsurePolicyIsUpdatedAsync(bus.Config.AdditionalSubscriberAccounts)
                .ConfigureAwait(false);

            await snsTopic.ApplyTagsAsync(cancellationToken).ConfigureAwait(false);

            eventPublisher.Arn = snsTopic.Arn;

            loggerFactory.CreateLogger<StaticPublicationConfiguration<TMessage>>().LogInformation(
                "Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'.",
                snsTopic.TopicName,
                typeof(TMessage));
        }

        return new StaticPublicationConfiguration<TMessage>(StartupTask, eventPublisher);
    }
}
