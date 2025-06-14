using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

#pragma warning disable CS0618

namespace JustSaying.Fluent;

internal sealed class StaticPublicationConfiguration(
    Func<CancellationToken, Task> startupTask,
    IMessagePublisher publisher,
    IMessageBatchPublisher batchPublisher) : ITopicPublisher
{
    public Func<CancellationToken, Task> StartupTask { get; } = startupTask;
    public IMessagePublisher Publisher { get; } = publisher;
    public IMessageBatchPublisher BatchPublisher { get; } = batchPublisher;

    public static StaticPublicationConfiguration Build<T>(
        string topicName,
        Dictionary<string, string> tags,
        SnsWriteConfiguration writeConfiguration,
        IAmazonSimpleNotificationService snsClient,
        ILoggerFactory loggerFactory,
        JustSayingBus bus) where T : Message
    {
        var readConfiguration = new SqsReadConfiguration(SubscriptionType.ToTopic)
        {
            TopicName = topicName
        };

        readConfiguration.ApplyTopicNamingConvention<T>(bus.Config.TopicNamingConvention);

        var compressionOptions = writeConfiguration.CompressionOptions ?? bus.Config.DefaultCompressionOptions;
        var serializer = bus.MessageBodySerializerFactory.GetSerializer<T>();
        var subjectProvider = bus.Config.MessageSubjectProvider;
        var subject = writeConfiguration.SubjectSet ? writeConfiguration.Subject : subjectProvider.GetSubjectForType(typeof(T));

        var eventPublisher = new SnsMessagePublisher(
            snsClient,
            new OutboundMessageConverter(PublishDestinationType.Topic, serializer, new MessageCompressionRegistry([new GzipMessageBodyCompression()]), compressionOptions, subject, writeConfiguration.IsRawMessage),
            loggerFactory,
            null,
            null)
        {
            MessageResponseLogger = bus.Config.MessageResponseLogger,
            MessageBatchResponseLogger = bus.PublishBatchConfiguration?.MessageBatchResponseLogger
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

        return new StaticPublicationConfiguration(StartupTask, eventPublisher, eventPublisher);
    }
}
