using System;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.Messaging.MessageHandling
{
    public class MessageContext
    {
        public MessageContext(SQSMessage message, Uri queueUri)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            QueueUri = queueUri ?? throw new ArgumentNullException(nameof(queueUri));
        }

        /// <summary>
        /// Gets the AWS SQS Message that is currently being processed.
        /// </summary>
        public SQSMessage Message { get; }

        /// <summary>
        /// Gets the SQS Queue that the message was received on.
        /// </summary>
        public Uri QueueUri { get; }
    }
}
