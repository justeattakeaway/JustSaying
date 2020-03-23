using Amazon.SQS.Model;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.AwsTools.MessageHandling
{
    internal static class SqsQueueBaseExtensions
    {
        internal static IQueueMessageContext ToMessageContext(this ISqsQueue sqsQueue, Message message)
        {
            return new QueueMessageContext(message, sqsQueue);
        }
    }
}
