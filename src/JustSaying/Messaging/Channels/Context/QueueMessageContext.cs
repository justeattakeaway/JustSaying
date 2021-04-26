using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels.Context
{
    /// <inheritdoc />
    public sealed class QueueMessageContext : IQueueMessageContext
    {
        private readonly SqsQueueReader _queueReader;

        /// <summary>
        /// A handle for a given message to be deleted from the queue it was read from.
        /// </summary>
        /// <param name="message">The <see cref="Amazon.SQS.Model.Message"/> to be handled.</param>
        /// <param name="queueReader">The <see cref="SqsQueueReader"/> the message was read from.</param>
        internal QueueMessageContext(Message message, SqsQueueReader queueReader)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            _queueReader = queueReader ?? throw new ArgumentNullException(nameof(queueReader));
        }

        /// <inheritdoc />
        public Message Message { get; }

        /// <inheritdoc />
        public Task DeleteMessageFromQueueAsync(CancellationToken cancellationToken)
        {
            return _queueReader.DeleteMessageAsync(Message.ReceiptHandle, cancellationToken);
        }

        /// <inheritdoc />
        public Task ChangeMessageVisibilityAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            return _queueReader.ChangeMessageVisibilityAsync(Message.ReceiptHandle, visibilityTimeout, cancellationToken);
        }

        /// <inheritdoc />
        public Uri QueueUri => _queueReader.Uri;

        /// <inheritdoc />
        public string QueueName => _queueReader.QueueName;
    }
}
