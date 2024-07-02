using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using Microsoft.Extensions.Logging;

#pragma warning disable CS0618

namespace JustSaying.Fluent;

internal sealed class StaticPublicationConfiguration(
    Func<CancellationToken, Task> startupTask,
    IMessagePublisher publisher) : ITopicPublisher
{
    public Func<CancellationToken, Task> StartupTask { get; } = startupTask;
    public IMessagePublisher Publisher { get; } = publisher;

    public static StaticPublicationConfiguration Build<T>(
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

        readConfiguration.ApplyTopicNamingConvention<T>(bus.Config.TopicNamingConvention);

        var eventPublisher = new SnsMessagePublisher(
            snsClient,
            bus.SerializationRegister,
            loggerFactory,
            bus.Config.MessageSubjectProvider)
        {
            MessageResponseLogger = bus.Config.MessageResponseLogger,
            CompressionRegistry = bus.CompressionRegistry,
            CompressionOptions = writeConfiguration.CompressionOptions,
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

            loggerFactory.CreateLogger<StaticPublicationConfiguration>().LogInformation(
                "Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'.",
                snsTopic.TopicName,
                typeof(T));
        }

        return new StaticPublicationConfiguration(StartupTask, eventPublisher);
    }
}
