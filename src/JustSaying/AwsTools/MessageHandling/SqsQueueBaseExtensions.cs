using System.Collections;
using System.Threading.Channels;
using Amazon.SQS.Model;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.AwsTools.MessageHandling
{
    internal static class SqsQueueBaseExtensions
    {
        internal static IQueueMessageContext ToMessageContext(this ISqsQueue sqsQueue, Message message)
        {
            var attributes = ExtractAttributes(message);
            return new QueueMessageContext(message, sqsQueue, attributes);
        }

        private static MessageAttributes ExtractAttributes(Message message)
        {
            var t = message.Body;
            return null;

        }
    }
}
