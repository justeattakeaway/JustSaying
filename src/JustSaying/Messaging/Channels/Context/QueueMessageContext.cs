using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels.Context
{
    public class QueueMessageContext : IQueueMessageContext
    {
        public QueueMessageContext(Message message, ISqsQueue sqsQueue, MessageAttributes messageAttributes)
        {
            Message = message;
            SqsQueue = sqsQueue;
            MessageAttributes = messageAttributes;
        }

        public Message Message { get; }

        public MessageAttributes MessageAttributes { get; }

        private ISqsQueue SqsQueue { get; }

        public async Task DeleteMessageFromQueueAsync()
        {
            await SqsQueue.DeleteMessageAsync(Message.ReceiptHandle).ConfigureAwait(false);
        }

        public async Task ChangeMessageVisibilityAsync(TimeSpan visibilityTimeout)
        {
            await SqsQueue.ChangeMessageVisibilityAsync(Message.ReceiptHandle, visibilityTimeout).ConfigureAwait(false);
        }

        public Uri QueueUri => SqsQueue.Uri;

        public string QueueName => SqsQueue.QueueName;
    }
}
