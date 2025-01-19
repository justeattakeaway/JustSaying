using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

internal sealed class StaticAddressPublicationConfiguration(
    IMessagePublisher publisher,
    IMessageBatchPublisher batchPublisher) : ITopicAddressPublisher
{
    public IMessagePublisher Publisher { get; } = publisher;
    public IMessageBatchPublisher BatchPublisher { get; } = batchPublisher;

    public static StaticAddressPublicationConfiguration Build<T>(
        string topicAddress,
        IAwsClientFactory clientFactory,
        ILoggerFactory loggerFactory,
        JustSayingBus bus)
    {
        var topicArn = Arn.Parse(topicAddress);

        var eventPublisher = new SnsMessagePublisher(
            topicAddress,
            clientFactory.GetSnsClient(RegionEndpoint.GetBySystemName(topicArn.Region)),
            bus.SerializationRegister,
            loggerFactory,
            bus.Config.MessageSubjectProvider)
        {
            MessageResponseLogger = bus.Config.MessageResponseLogger,
            MessageBatchResponseLogger = bus.PublishBatchConfiguration?.MessageBatchResponseLogger
        };

        return new StaticAddressPublicationConfiguration(eventPublisher, eventPublisher);
    }
}
