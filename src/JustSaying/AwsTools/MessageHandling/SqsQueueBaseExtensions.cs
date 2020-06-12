using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.AwsTools.MessageHandling
{
    internal static class SqsQueueBaseExtensions
    {
        internal static IQueueMessageContext ToMessageContext(this ISqsQueue sqsQueue, Message message)
        {
            return new QueueMessageContext(message, sqsQueue);
        }

        public static async Task<IList<Message>> GetMessagesAsync(
            this ISqsQueue sqsQueue,
            int maximumCount,
            IEnumerable<string> requestMessageAttributeNames,
            CancellationToken cancellationToken)
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = sqsQueue.Uri.AbsoluteUri,
                MaxNumberOfMessages = maximumCount,
                WaitTimeSeconds = 20,
                AttributeNames = requestMessageAttributeNames.ToList()
            };

            ReceiveMessageResponse sqsMessageResponse =
                await sqsQueue.Client.ReceiveMessageAsync(request, cancellationToken).ConfigureAwait(false);

            return sqsMessageResponse?.Messages;
        }

        public static async Task DeleteMessageAsync(
            this ISqsQueue sqsQueue,
            string receiptHandle,
            CancellationToken cancellationToken)
        {
            var deleteRequest = new DeleteMessageRequest
            {
                QueueUrl = sqsQueue.Uri.AbsoluteUri,
                ReceiptHandle = receiptHandle,
            };

            await sqsQueue.Client.DeleteMessageAsync(deleteRequest, cancellationToken).ConfigureAwait(false);
        }

        public static async Task ChangeMessageVisibilityAsync(
            this ISqsQueue sqsQueue,
            string receiptHandle,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var visibilityRequest = new ChangeMessageVisibilityRequest
            {
                QueueUrl = sqsQueue.Uri.ToString(),
                ReceiptHandle = receiptHandle,
                VisibilityTimeout = (int)timeout.TotalSeconds,
            };

            await sqsQueue.Client.ChangeMessageVisibilityAsync(visibilityRequest, cancellationToken).ConfigureAwait(false);
        }
    }
}
