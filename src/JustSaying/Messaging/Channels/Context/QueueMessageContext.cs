using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels.Context
{
    public class QueueMessageContext : IQueueMessageContext
    {
        private ISqsQueue _sqsQueue;

        public QueueMessageContext(Message message, ISqsQueue sqsQueue)
        {
            Message = message;
            _sqsQueue = sqsQueue;
        }

        public Message Message { get; }

        public async Task DeleteMessageFromQueueAsync()
        {
            await _sqsQueue.DeleteMessageAsync(Message.ReceiptHandle).ConfigureAwait(false);
        }

        public async Task ChangeMessageVisibilityAsync(TimeSpan visibilityTimeout)
        {
            await _sqsQueue.ChangeMessageVisibilityAsync(Message.ReceiptHandle, visibilityTimeout).ConfigureAwait(false);
        }

        public Uri QueueUri => _sqsQueue.Uri;

        public string QueueName => _sqsQueue.QueueName;
    }
}
