using System;
using JustSaying.Messaging.Channels.Context;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.Messaging.MessageHandling
{
    /// <summary>
    /// Context metadata about the SQS message currently being processed.
    /// </summary>
    public class MessageContext
    {
        /// <summary>
        /// Creates an instance of <see cref="MessageContext"/>.
        /// </summary>
        /// <param name="message">The <see cref="Amazon.SQS.Model.Message"/> currently being processed.</param>
        /// <param name="queueUri">The URI of the SQS queue the message is from.</param>
        /// <param name="messageAttributes">The <see cref="MessageAttributes"/> from the message.</param>
        public MessageContext(SQSMessage message, Uri queueUri, MessageAttributes messageAttributes)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            QueueUri = queueUri ?? throw new ArgumentNullException(nameof(queueUri));
            MessageAttributes = messageAttributes ?? throw new ArgumentNullException(nameof(messageAttributes));
        }

        /// <summary>
        /// Gets the AWS SQS Message that is currently being processed.
        /// </summary>
        public SQSMessage Message { get; }

        /// <summary>
        /// Gets the SQS Queue that the message was received on.
        /// </summary>
        public Uri QueueUri { get; }

        /// <summary>
        /// Gets a collection of <see cref="MessageAttributeValue"/>'s that were sent with this message
        /// </summary>
        public MessageAttributes MessageAttributes { get; }
    }
}
