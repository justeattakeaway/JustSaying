using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// An SNS message publisher for a <see cref="TopicAddress"/>.
/// </summary>
internal sealed class TopicAddressPublisher : SnsMessagePublisher
{
    public TopicAddressPublisher(IAmazonSimpleNotificationService snsClient,
        ILoggerFactory loggerFactory,
        IMessageSubjectProvider subjectProvider,
        IMessageSerializationRegister serializationRegister,
        Func<Exception, Message, bool> handleException,
        Func<Exception, IReadOnlyCollection<Message>, bool> handleBatchException,
        TopicAddress topicAddress)
        : base(topicAddress.TopicArn, snsClient, serializationRegister, loggerFactory, subjectProvider, handleException, handleBatchException)
    { }
}
