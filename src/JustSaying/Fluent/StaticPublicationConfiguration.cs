using Amazon;
using Amazon.Internal;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
#pragma warning disable CS0618

namespace JustSaying.Fluent;

internal class StaticPublicationConfiguration
{
    public Func<CancellationToken, Task> StartupTask { get; }
    public IMessagePublisher Publisher { get; }
    public string TopicName { get; }

    public StaticPublicationConfiguration(Func<CancellationToken, Task> startupTask, IMessagePublisher publisher, string topicName)
    {
        StartupTask = startupTask;
        Publisher = publisher;
        TopicName = topicName;
    }

    public static StaticPublicationConfiguration Build<T>(string topicName, Dictionary<string, string> tags, string region, SnsWriteConfiguration writeConfiguration, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory, JustSayingBus bus)
    {
        var readConfiguration = new SqsReadConfiguration(SubscriptionType.ToTopic)
        {
            TopicName = topicName
        };

        readConfiguration.ApplyTopicNamingConvention<T>(bus.Config.TopicNamingConvention);

        var eventPublisher = new SnsMessagePublisher(
            proxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(region)),
            bus.SerializationRegister,
            loggerFactory,
            bus.Config.MessageSubjectProvider)
        {
            MessageResponseLogger = bus.Config.MessageResponseLogger,
        };

#pragma warning disable 618
        var snsTopic = new SnsTopicByName(
            readConfiguration.TopicName,
            proxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(region)),
            loggerFactory)
        {
            Tags = tags
        };
#pragma warning restore 618

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
        }

        return new StaticPublicationConfiguration(StartupTask, eventPublisher, snsTopic.TopicName);
    }
}
