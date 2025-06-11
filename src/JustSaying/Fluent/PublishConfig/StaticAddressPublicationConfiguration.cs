using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Models;
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
        IOutboundMessageConverter messageConverter,
        ILoggerFactory loggerFactory,
        JustSayingBus bus,
        Func<Exception, Message, bool> exceptionHandler,
        Func<Exception, IReadOnlyCollection<Message>, bool> exceptionBatchHandler)
    {
        var topicArn = Arn.Parse(topicAddress);

        var eventPublisher = new SnsMessagePublisher(
            topicAddress,
            clientFactory.GetSnsClient(RegionEndpoint.GetBySystemName(topicArn.Region)),
            messageConverter,
            loggerFactory,
            exceptionHandler,
            exceptionBatchHandler)
        {
            MessageResponseLogger = bus.Config.MessageResponseLogger,
            MessageBatchResponseLogger = bus.PublishBatchConfiguration?.MessageBatchResponseLogger
        };

        return new StaticAddressPublicationConfiguration(eventPublisher, eventPublisher);
    }
}
