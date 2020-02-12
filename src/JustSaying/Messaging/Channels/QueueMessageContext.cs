using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels
{
    public class QueueMessageContext : IQueueMessageContext
    {
        public QueueMessageContext(Message message, ISqsQueue sqsQueue)
        {
            Message = message;
            SqsQueue = sqsQueue;
        }

        public Message Message { get; }

        private ISqsQueue SqsQueue { get; }

        public async Task DeleteMessageFromQueue()
        {
            await SqsQueue.DeleteMessageAsync(Message.ReceiptHandle).ConfigureAwait(false);
        }

        public async Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(int visibilityTimeoutSeconds)
        {
            var visibilityRequest = new ChangeMessageVisibilityRequest
            {
                QueueUrl = QueueUri.AbsoluteUri,
                ReceiptHandle = Message.ReceiptHandle,
                VisibilityTimeout = visibilityTimeoutSeconds
            };

            return await SqsQueue.ChangeMessageVisibilityAsync(visibilityRequest).ConfigureAwait(false);
        }

        public Uri QueueUri => SqsQueue.Uri;
    }
}
