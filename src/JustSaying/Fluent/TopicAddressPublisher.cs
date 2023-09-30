using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// An SNS message publisher for a <see cref="TopicAddress"/>.
/// </summary>
internal sealed class TopicAddressPublisher(
    IAmazonSimpleNotificationService snsClient,
    ILoggerFactory loggerFactory,
    IMessageSubjectProvider subjectProvider,
    IMessageSerializationRegister serializationRegister,
    Func<Exception, Message, bool> handleException,
    TopicAddress topicAddress) : SnsMessagePublisher(topicAddress.TopicArn, snsClient, serializationRegister, loggerFactory, subjectProvider, handleException)
{
}
