using System;
using Amazon.SimpleNotificationService;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    ///
    /// </summary>
    internal sealed class TopicAddressPublisher : SnsMessagePublisher
    {
        public TopicAddressPublisher(IAmazonSimpleNotificationService snsClient, ILoggerFactory loggerFactory, IMessageSubjectProvider subjectProvider, IMessageSerializationRegister serializationRegister, Func<Exception, Message, bool> handleException, TopicAddress topicAddress)
            : base(topicAddress.TopicArn, snsClient, serializationRegister, loggerFactory, subjectProvider, handleException)
        { }
    }
}
