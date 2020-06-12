using System;
using System.Threading;
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

        public async Task DeleteMessageFromQueueAsync(CancellationToken cancellationToken)
        {
            await _sqsQueue.DeleteMessageAsync(Message.ReceiptHandle, cancellationToken).ConfigureAwait(false);
        }

        public async Task ChangeMessageVisibilityAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            await _sqsQueue.ChangeMessageVisibilityAsync(Message.ReceiptHandle, visibilityTimeout, cancellationToken).ConfigureAwait(false);
        }

        public Uri QueueUri => _sqsQueue.Uri;

        public string QueueName => _sqsQueue.QueueName;
    }
}
