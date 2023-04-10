using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// An SNS message publisher for a <see cref="TopicAddress"/>.
/// </summary>
internal sealed class TopicAddressPublisher<TMessage> : SnsMessagePublisher<TMessage> where TMessage : class
{
    public TopicAddressPublisher(IAmazonSimpleNotificationService snsClient, ILoggerFactory loggerFactory, IMessageSubjectProvider subjectProvider, IMessageSerializationRegister serializationRegister, Func<Exception, TMessage, bool> handleException, TopicAddress topicAddress)
        : base(topicAddress.TopicArn, snsClient, serializationRegister, loggerFactory, subjectProvider, handleException)
    { }
}
